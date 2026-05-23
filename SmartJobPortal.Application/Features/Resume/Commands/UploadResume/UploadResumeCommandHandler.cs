using DocumentFormat.OpenXml.Packaging;
using MediatR;
using Microsoft.Extensions.Configuration;
using SmartJobPortal.Application.Common;
using SmartJobPortal.Application.DTOs.Candidate;
using SmartJobPortal.Application.Interfaces;
using SmartJobPortal.Application.Features.Resume.Common;
using SmartJobPortal.Domain.Entities;
using System.Text;
using System.Text.RegularExpressions;
using UglyToad.PdfPig;
using Microsoft.AspNetCore.Http;

namespace SmartJobPortal.Application.Features.Resume.Commands.UploadResume;

public class UploadResumeCommandHandler : IRequestHandler<UploadResumeCommand, ApiResponse<ResumeParseResponse>>
{
    private readonly ICandidateRepository _candidateRepo;
    private readonly IUserRepository _userRepo;
    private readonly ICacheService _cache;
    private readonly IConfiguration _config;
    private readonly IParsedResumeRepository _parsedResumeRepo;
    private readonly IAffindaParsingService _affindaService;

    private readonly ResumeFileValidator _validator;
    private readonly ResumeSkillExtractor _extractor;

    public UploadResumeCommandHandler(
        ICandidateRepository candidateRepo,
        IUserRepository userRepo,
        ICacheService cache,
        IConfiguration config,
        IParsedResumeRepository parsedResumeRepo,
        IAffindaParsingService affindaService)

    {
        _candidateRepo = candidateRepo;
        _userRepo = userRepo;
        _parsedResumeRepo = parsedResumeRepo;
        _affindaService = affindaService;
        _cache = cache;
        _config = config;

        _validator = new ResumeFileValidator();
        _extractor = new ResumeSkillExtractor();
    }

    public async Task<ApiResponse<ResumeParseResponse>> Handle(UploadResumeCommand request, CancellationToken cancellationToken)
    {
        var userId = request.UserId;
        var file = request.File;

        // ── Layer 1: File validation ─────────────────────────────
        var (isValid, validationError) = _validator.Validate(file);
        if (!isValid)
            return ApiResponse<ResumeParseResponse>.Fail(validationError ?? "Validation failed.");

        // Local text extraction removed to prevent blocking complex PDFs.
        // File validation is sufficient. Affinda will handle text extraction.

        // ── Save file ────────────────────────────────────────────
        var storagePath = _config["ResumeStorage:Path"] ?? "uploads/resumes";
        Directory.CreateDirectory(storagePath);

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        var safeFile = $"{userId}_{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}{ext}";
        var filePath = Path.Combine(storagePath, safeFile);

        await using (var stream = File.Create(filePath))
        {
            file.OpenReadStream().Position = 0;
            await file.CopyToAsync(stream);
        }



        // ── Update or Create records ────────────────────────────
        var existingCandidate = await _candidateRepo.GetByUserIdAsync(userId);
        var candidate = existingCandidate ?? new SmartJobPortal.Domain.Entities.Candidate { UserId = userId };
        var user = await _userRepo.GetByIdAsync(userId);

        candidate.ResumeFilePath = filePath;
        candidate.ResumeOriginalName = SanitiseFileName(file.FileName);
        candidate.ResumeUploadedAt = DateTime.Now;

        var candidateId = await _candidateRepo.UpsertAsync(candidate);
        candidate.CandidateId = candidateId;

        // Insert ParsedResume entry with proper CandidateId
        var parsedResume = new ParsedResume
        {
            CandidateId = candidateId,
            FileName = file.FileName,
            FilePath = filePath,
            Status = "Pending",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _parsedResumeRepo.CreateAsync(parsedResume);

        // ── Parse with Affinda synchronously ────────────────────────────
        var affindaResult = await _affindaService.ParseAsync(filePath, cancellationToken);
        
        var resumeDto = new ResumeDto
        {
            FullName = user?.FullName ?? candidate.ResumeOriginalName,
            Email = user?.Email,
            Phone = user?.PhoneNumber,
            LinkedIn = candidate.LinkedInUrl,
            GitHub = candidate.GitHubUrl,
            LeetCode = candidate.LeetCodeUrl,
            Skills = new(),
            Education = new(),
            WorkExperience = new()
        };

        if (affindaResult != null)
        {
            if (!string.IsNullOrEmpty(affindaResult.FullName)) resumeDto.FullName = affindaResult.FullName;
            if (!string.IsNullOrEmpty(affindaResult.Email)) resumeDto.Email = affindaResult.Email;
            if (!string.IsNullOrEmpty(affindaResult.Phone)) resumeDto.Phone = affindaResult.Phone;
            
            if (affindaResult.Skills != null && affindaResult.Skills.Any())
            {
                resumeDto.Skills = affindaResult.Skills;
                await MergeSkillsAsync(candidateId, affindaResult.Skills);
            }
            if (affindaResult.Education != null && affindaResult.Education.Any())
            {
                resumeDto.Education = affindaResult.Education;
                await MergeEducationAsync(candidateId, affindaResult.Education);
            }
            if (affindaResult.WorkExperience != null && affindaResult.WorkExperience.Any())
            {
                resumeDto.WorkExperience = affindaResult.WorkExperience;
                await MergeExperienceAsync(candidateId, affindaResult.WorkExperience);
            }
            
            parsedResume.Status = "Completed";
            parsedResume.ParsedJson = System.Text.Json.JsonSerializer.Serialize(affindaResult);
            parsedResume.UpdatedAt = DateTime.UtcNow;
            await _parsedResumeRepo.UpdateAsync(parsedResume);
        }
        else
        {
            parsedResume.Status = "Failed";
            parsedResume.ErrorMessage = "Affinda returned null";
            parsedResume.UpdatedAt = DateTime.UtcNow;
            await _parsedResumeRepo.UpdateAsync(parsedResume);
        }

        await _cache.RemoveAsync($"candidate:profile:{userId}");

        return ApiResponse<ResumeParseResponse>.Ok(new ResumeParseResponse
        {
            Message = "Resume uploaded and parsed successfully.",
            ParsedData = resumeDto
        }, "Resume uploaded and parsed successfully.");
    }



    private async Task MergeSkillsAsync(int candidateId, List<string> skills)
    {
        var existing = (await _candidateRepo.GetSkillsAsync(candidateId)).Select(s => s.SkillName.ToLower()).ToHashSet();
        foreach (var skill in skills.Where(s => !existing.Contains(s.ToLower())))
        {
            var id = await _candidateRepo.GetSkillIdByNameAsync(skill) ?? await _candidateRepo.CreateSkillAsync(skill);
            await _candidateRepo.AddCandidateSkillAsync(candidateId, id, "Intermediate");
        }
    }

    private async Task MergeEducationAsync(int candidateId, List<EducationDto> education)
    {
        var existing = (await _candidateRepo.GetEducationAsync(candidateId))
            .Select(e => (e.Degree ?? "").ToLower())
            .ToHashSet();

        var toAdd = education
            .Where(e => !string.IsNullOrEmpty(e.Degree) && !existing.Contains(e.Degree.ToLower()))
            .Select(e => new CandidateEducation
        {
            CandidateId = candidateId,
            Degree = e.Degree ?? string.Empty,
            Institution = e.Institution ?? string.Empty,
            GraduationYear = e.Year ?? string.Empty
        }).ToList();
        if (toAdd.Any()) await _candidateRepo.AddEducationAsync(toAdd);
    }

    private async Task MergeExperienceAsync(int candidateId, List<ExperienceDto> experience)
    {
        var existing = (await _candidateRepo.GetExperienceAsync(candidateId))
            .Select(e => (e.Role ?? "").ToLower())
            .ToHashSet();

        var toAdd = experience
            .Where(e => !string.IsNullOrEmpty(e.Role) && !existing.Contains(e.Role.ToLower()))
            .Select(e => new CandidateExperience
        {
            CandidateId = candidateId,
            Company = e.Company ?? string.Empty,
            Role = e.Role ?? string.Empty,
            Duration = e.Duration ?? string.Empty,
            Description = e.Description ?? string.Empty
        }).ToList();
        if (toAdd.Any()) await _candidateRepo.AddExperienceAsync(toAdd);
    }

    private static string SanitiseFileName(string filename)
    {
        var safe = Path.GetFileNameWithoutExtension(filename);
        safe = Regex.Replace(safe, @"[^a-zA-Z0-9\-_\s]", "");
        var ext = Path.GetExtension(filename).ToLowerInvariant();
        return (safe.Length > 50 ? safe[..50] : safe) + ext;
    }
}
