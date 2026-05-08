using DocumentFormat.OpenXml.Packaging;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using SmartJobPortal.Application.Common;
using SmartJobPortal.Application.DTOs.Candidate;
using SmartJobPortal.Application.Interfaces;
using SmartJobPortal.Application.Services.ResumeLogic;
using SmartJobPortal.Domain.Entities;
using System.Text;
using System.Text.RegularExpressions;
using UglyToad.PdfPig;

namespace SmartJobPortal.Application.Services;

public class ResumeService : IResumeService
{
    private readonly ICandidateRepository _candidateRepo;
    private readonly IUserRepository _userRepo;
    private readonly ICacheService _cache;
    private readonly IConfiguration _config;
    private readonly IGeminiService _gemini;
    private readonly ResumeFileValidator _validator;
    private readonly ResumeSkillExtractor _extractor;

    public ResumeService(
        ICandidateRepository candidateRepo,
        IUserRepository userRepo,
        ICacheService cache,
        IConfiguration config,
        IGeminiService gemini)
    {
        _candidateRepo = candidateRepo;
        _userRepo = userRepo;
        _cache = cache;
        _config = config;
        _gemini = gemini;
        _validator = new ResumeFileValidator();
        _extractor = new ResumeSkillExtractor();
    }

    public async Task<ApiResponse<ResumeParseResponse>> UploadAndParseAsync(
        int userId, IFormFile file)
    {
        // ── Layer 1: File validation ─────────────────────────────
        var (isValid, validationError) = _validator.Validate(file);
        if (!isValid)
            return ApiResponse<ResumeParseResponse>.Fail(validationError);

        // ── Layer 2: Extract raw text ────────────────────────────
        string rawText;
        try
        {
            rawText = await ExtractTextAsync(file);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Resume] Text extraction failed: {ex.Message}");
            return ApiResponse<ResumeParseResponse>.Fail(
                "Could not read the file. Ensure it is a valid PDF or DOCX.");
        }

        // ── Layer 3: Sanitise (kills prompt injections) ──────────
        var sanitisedText = ResumeTextSanitiser.Sanitise(rawText);

        if (string.IsNullOrWhiteSpace(sanitisedText))
        {
            Console.WriteLine($"[SECURITY] Sanitised text is empty for user {userId}. Original length: {rawText?.Length ?? 0}");
            return ApiResponse<ResumeParseResponse>.Fail(
                "Your resume could not be processed. Please ensure it " +
                "contains only professional content and try again.");
        }

        if (ResumeTextSanitiser.IsSuspicious(sanitisedText))
        {
            return ApiResponse<ResumeParseResponse>.Fail(
                "Your resume content appears invalid. " +
                "Please upload a standard resume document.");
        }

        // ── Layer 4: Hybrid Extraction ───────────────────────────
        
        // 4.1: Whitelist Skills (Fast, 100% Secure)
        var extraction = _extractor.Extract(sanitisedText);
        if (!extraction.IsValidResume)
        {
            return ApiResponse<ResumeParseResponse>.Fail(
                "This document does not appear to be a resume. " +
                "Please upload a resume with your professional experience.");
        }

        // 4.2: AI Extraction for Complex Data (Company, Institution, Dates)
        // Only sent AFTER sanitisation
        var aiData = await _gemini.ExtractStructuredDataAsync(sanitisedText);

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
        var candidate = existingCandidate ?? new Candidate { UserId = userId };
        var user = await _userRepo.GetByIdAsync(userId);

        candidate.ResumeFilePath = filePath;
        candidate.ResumeOriginalName = SanitiseFileName(file.FileName);
        candidate.ResumeUploadedAt = DateTime.Now;

        // Use AI results to enrich profile if available
        if (aiData != null && user != null)
        {
            var newName = !string.IsNullOrEmpty(aiData.FullName) ? aiData.FullName : user.FullName;
            var newPhone = !string.IsNullOrEmpty(aiData.Phone) ? aiData.Phone : user.PhoneNumber;
            
            if (newName != user.FullName || newPhone != user.PhoneNumber)
            {
                await _userRepo.UpdateProfileAsync(userId, newName, newPhone);
                // Refresh user object after update
                user = await _userRepo.GetByIdAsync(userId);
            }
        }

        // Use AI experience if available, else fallback to whitelist
        var finalExp = aiData?.TotalExperience > 0 ? aiData.TotalExperience : extraction.ExperienceYears;
        if (finalExp > 0 && candidate.ExperienceYears == 0)
            candidate.ExperienceYears = (int)finalExp;

        var candidateId = await _candidateRepo.UpsertAsync(candidate);
        candidate.CandidateId = candidateId;

        // ── Merge Data ──────────────────────────────────────────
        
        // 1: Merge Skills (Combine AI results with Whitelist fallback)
        var allSkills = new HashSet<string>(extraction.Skills, StringComparer.OrdinalIgnoreCase);
        if (aiData?.Skills != null)
        {
            foreach (var s in aiData.Skills) allSkills.Add(s);
        }

        if (allSkills.Any())
            await MergeSkillsAsync(candidateId, allSkills.ToList());

        // 2: Merge Education (from AI)
        if (aiData?.Education?.Any() == true)
            await MergeEducationAsync(candidateId, aiData.Education);

        // 3: Merge Experience (from AI)
        if (aiData?.WorkExperience?.Any() == true)
            await MergeExperienceAsync(candidateId, aiData.WorkExperience);

        await _cache.RemoveAsync($"candidate:profile:{userId}");

        return ApiResponse<ResumeParseResponse>.Ok(new ResumeParseResponse
            {
                Message = "Resume processed with AI enhancement.",
                ParsedData = new ResumeDto
                {
                    FullName = user?.FullName ?? candidate.ResumeOriginalName,
                    Email = user?.Email ?? aiData?.Email ?? extraction.Email,
                    Phone = user?.PhoneNumber ?? aiData?.Phone,
                    Skills = allSkills.ToList(),
                    TotalExperience = finalExp,
                    Education = aiData?.Education ?? new(),
                    WorkExperience = aiData?.WorkExperience ?? new()
                }
            }, "Resume processed successfully.");
    }

    public async Task<(byte[] bytes, string contentType, string fileName)?> GetResumeFileAsync(int userId)
    {
        var candidate = await _candidateRepo.GetByUserIdAsync(userId);
        if (candidate == null || string.IsNullOrEmpty(candidate.ResumeFilePath) || !File.Exists(candidate.ResumeFilePath))
            return null;

        var bytes = await File.ReadAllBytesAsync(candidate.ResumeFilePath);
        var ext = Path.GetExtension(candidate.ResumeFilePath).ToLowerInvariant();
        var contentType = ext == ".pdf" ? "application/pdf" : "application/vnd.openxmlformats-officedocument.wordprocessingml.document";
        return (bytes, contentType, candidate.ResumeOriginalName);
    }

    public async Task<bool> HasResumeAsync(int userId)
    {
        var candidate = await _candidateRepo.GetByUserIdAsync(userId);
        return candidate != null && !string.IsNullOrEmpty(candidate.ResumeFilePath) && File.Exists(candidate.ResumeFilePath);
    }

    public async Task<ApiResponse<bool>> DeleteResumeAsync(int userId)
    {
        var candidate = await _candidateRepo.GetByUserIdAsync(userId);
        if (candidate == null || string.IsNullOrEmpty(candidate.ResumeFilePath))
            return ApiResponse<bool>.Fail("No resume found.");

        try { if (File.Exists(candidate.ResumeFilePath)) File.Delete(candidate.ResumeFilePath); } catch { }

        candidate.ResumeFilePath = string.Empty;
        candidate.ResumeOriginalName = string.Empty;
        candidate.ResumeUploadedAt = null;
        await _candidateRepo.UpsertAsync(candidate);
        await _cache.RemoveAsync($"candidate:profile:{userId}");
        return ApiResponse<bool>.Ok(true, "Deleted.");
    }

    private static async Task<string> ExtractTextAsync(IFormFile file)
    {
        await using var stream = file.OpenReadStream();
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (ext == ".pdf")
        {
            using var doc = PdfDocument.Open(stream);
            var sb = new StringBuilder();
            foreach (var page in doc.GetPages()) sb.AppendLine(page.Text);
            return sb.ToString();
        }
        using var wordDoc = WordprocessingDocument.Open(stream, false);
        return wordDoc.MainDocumentPart?.Document.Body?.InnerText ?? string.Empty;
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
            Degree = e.Degree,
            Institution = e.Institution,
            GraduationYear = e.Year
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
            Company = e.Company,
            Role = e.Role,
            Duration = e.Duration,
            Description = e.Description
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