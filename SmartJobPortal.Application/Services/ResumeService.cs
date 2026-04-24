using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using SmartJobPortal.Application.Common;
using SmartJobPortal.Application.DTOs.Candidate;
using SmartJobPortal.Application.Interfaces;
using SmartJobPortal.Domain.Entities;

namespace SmartJobPortal.Application.Services;

public class ResumeService : IResumeService
{
    private readonly ICandidateRepository _candidateRepo;
    private readonly ICacheService _cache;
    private readonly IResumeParserService _parser;
    private readonly IJobSearchService _jobSearchService;
    private readonly string _uploadPath;

    public ResumeService(
        ICandidateRepository candidateRepo,
        ICacheService cache,
        IResumeParserService parser,
        IJobSearchService jobSearchService,
        IConfiguration config)
    {
        _candidateRepo = candidateRepo;
        _cache = cache;
        _parser = parser;
        _jobSearchService = jobSearchService;
        _uploadPath = config["ResumeStorage:Path"] ?? "uploads/resumes";
    }

    public async Task<ApiResponse<ResumeParseResponse>> UploadAndParseAsync(int userId, IFormFile file)
    {
        //  Validate 
        var allowed = new[]
        {
            "application/pdf",
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
        };

        if (!allowed.Contains(file.ContentType))
            return ApiResponse<ResumeParseResponse>.Fail("Only PDF and DOCX files are accepted.");

        var candidate = await _candidateRepo.GetByUserIdAsync(userId);
        if (candidate == null)
            return ApiResponse<ResumeParseResponse>.Fail("Complete your profile before uploading a resume.");

        //  Save file 
        Directory.CreateDirectory(_uploadPath);
        var ext = Path.GetExtension(file.FileName);
        var fileName = $"{userId}_{DateTime.Now:yyyyMMddHHmmss}{ext}";
        var fullPath = Path.Combine(_uploadPath, fileName);

        await using (var stream = File.Create(fullPath))
            await file.CopyToAsync(stream);

        //  AI Extraction 
        var parsedData = await _parser.ParseResumeAsync(fullPath, file.ContentType);
        if (parsedData == null)
            return ApiResponse<ResumeParseResponse>.Fail("AI could not parse the resume. Please try a different file.");

        //  Persist Structured Data 
        // Skills
        var existingSkills = await _candidateRepo.GetSkillsAsync(candidate.CandidateId);
        var existingIds = existingSkills.Select(s => s.SkillId).ToHashSet();
        var newRows = new List<CandidateSkill>(existingSkills);

        foreach (var skillName in parsedData.Skills)
        {
            var skillId = await _candidateRepo.GetSkillIdByNameAsync(skillName)
                          ?? await _candidateRepo.CreateSkillAsync(skillName);

            if (!existingIds.Contains(skillId))
            {
                newRows.Add(new CandidateSkill { CandidateId = candidate.CandidateId, SkillId = skillId, Level = "Intermediate" });
                existingIds.Add(skillId);
            }
        }
        await _candidateRepo.ReplaceSkillsAsync(candidate.CandidateId, newRows);

        // Education & Experience (Clear and Reload)
        await _candidateRepo.ClearEducationAndExperienceAsync(candidate.CandidateId);
        
        var education = parsedData.Education.Select(e => new CandidateEducation
        {
            CandidateId = candidate.CandidateId,
            Degree = e.Degree ?? "N/A",
            Institution = e.Institution ?? "N/A",
            GraduationYear = e.Year
        }).ToList();
        await _candidateRepo.AddEducationAsync(education);

        var experience = parsedData.WorkExperience.Select(e => new CandidateExperience
        {
            CandidateId = candidate.CandidateId,
            Company = e.Company ?? "N/A",
            Role = e.Role ?? "N/A",
            Duration = e.Duration,
            Description = e.Description
        }).ToList();
        await _candidateRepo.AddExperienceAsync(experience);

        // Update Candidate Profile Basics
        candidate.Summary = $"Experience: {parsedData.TotalExperience} years. " + string.Join(". ", parsedData.WorkExperience.Select(e => $"{e.Role} at {e.Company}"));
        candidate.Headline = parsedData.WorkExperience.FirstOrDefault()?.Role ?? "Professional";
        candidate.ExperienceYears = (int)Math.Round(parsedData.TotalExperience);
        candidate.ResumeFilePath = fullPath;
        candidate.ResumeOriginalName = file.FileName;
        candidate.ResumeUploadedAt = DateTime.Now;
        candidate.UpdatedAt = DateTime.Now;

        await _candidateRepo.UpsertAsync(candidate);

        //  Job Matching 
        var recommendedJobs = await _jobSearchService.RecommendJobsBySkillsAsync(userId, parsedData.Skills);

        //  Bust Cache 
        await _cache.RemoveAsync($"candidate:profile:{userId}");

        return ApiResponse<ResumeParseResponse>.Ok(new ResumeParseResponse
        {
            ParsedData = parsedData,
            RecommendedJobs = recommendedJobs,
            Message = "AI successfully parsed your resume and updated your profile!"
        });
    }
}