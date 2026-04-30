using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using SmartJobPortal.Application.Common;
using SmartJobPortal.Application.DTOs.Candidate;
using SmartJobPortal.Application.Interfaces;
using SmartJobPortal.Domain.Entities;
using UglyToad.PdfPig;
using System.Globalization;
using System.IO.Compression;
using System.Text;
using System.Xml.Linq;

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
        try
        {
            // ✅ Validate
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

            // Save file
            Directory.CreateDirectory(_uploadPath);
            var ext = Path.GetExtension(file.FileName);
            var fileName = $"{userId}_{DateTime.Now:yyyyMMddHHmmss}{ext}";
            var fullPath = Path.Combine(_uploadPath, fileName);

            await using (var stream = File.Create(fullPath))
                await file.CopyToAsync(stream);

            // Extract text
            string resumeText = file.ContentType switch
            {
                "application/pdf" => ExtractTextFromPdf(fullPath),
                _ => ExtractTextFromDocx(fullPath)
            };

            File.WriteAllText("resume_text_debug.txt", resumeText ?? "EMPTY TEXT");

            if (string.IsNullOrWhiteSpace(resumeText))
                return ApiResponse<ResumeParseResponse>.Fail("Could not extract text from resume.");

            // AI Parsing
            var parsedData = await _parser.ParseResumeAsync(resumeText);

            if (parsedData == null)
            {
                File.WriteAllText("ai_null.txt", "AI returned NULL");
                return ApiResponse<ResumeParseResponse>.Fail("AI could not parse the resume.");
            }

            // Null safety
            parsedData.Skills ??= new List<string>();
            parsedData.Education ??= new List<EducationDto>();
            parsedData.WorkExperience ??= new List<ExperienceDto>();

            // Normalize skills
            var normalizedSkills = parsedData.Skills
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(NormalizeSkill)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            File.WriteAllText("normalized_skills.txt", string.Join(", ", normalizedSkills));

            // Save Skills
            var existingSkills = await _candidateRepo.GetSkillsAsync(candidate.CandidateId);
            var existingIds = existingSkills.Select(s => s.SkillId).ToHashSet();
            var newRows = new List<CandidateSkill>(existingSkills);

            foreach (var skill in normalizedSkills)
            {
                var skillId = await _candidateRepo.GetSkillIdByNameAsync(skill)
                              ?? await _candidateRepo.CreateSkillAsync(skill);

                if (!existingIds.Contains(skillId))
                {
                    newRows.Add(new CandidateSkill
                    {
                        CandidateId = candidate.CandidateId,
                        SkillId = skillId,
                        Level = "Intermediate"
                    });

                    existingIds.Add(skillId);
                }
            }

            await _candidateRepo.ReplaceSkillsAsync(candidate.CandidateId, newRows);

            // Education & Experience
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

            // Profile update
            candidate.ExperienceYears = parsedData.TotalExperience > 0
                ? (int)Math.Round(parsedData.TotalExperience)
                : 0;

            candidate.Summary =
                $"Experience: {parsedData.TotalExperience} years. " +
                string.Join(". ", parsedData.WorkExperience.Select(e => $"{e.Role} at {e.Company}"));

            candidate.Headline = parsedData.WorkExperience.FirstOrDefault()?.Role ?? "Professional";

            candidate.ResumeFilePath = fullPath;
            candidate.ResumeOriginalName = file.FileName;
            candidate.ResumeUploadedAt = DateTime.Now;
            candidate.UpdatedAt = DateTime.Now;

            await _candidateRepo.UpsertAsync(candidate);

            // Job matching
            var recommendedJobs = await _jobSearchService
                .RecommendJobsBySkillsAsync(userId, normalizedSkills);

            // Clear cache
            await _cache.RemoveAsync($"candidate:profile:{userId}");

            File.WriteAllText("parsed_result.json",
                System.Text.Json.JsonSerializer.Serialize(parsedData));

            return ApiResponse<ResumeParseResponse>.Ok(new ResumeParseResponse
            {
                ParsedData = parsedData,
                RecommendedJobs = recommendedJobs,
                Message = "Resume parsed successfully 🎉"
            });
        }
        catch (Exception ex)
        {
            File.WriteAllText("resume_error.txt", ex.ToString());
            return ApiResponse<ResumeParseResponse>.Fail("Something went wrong while processing resume.");
        }
    }

    // REQUIRED METHODS (FIXED YOUR ERROR)

    public async Task<(byte[] bytes, string contentType, string fileName)?> GetResumeFileAsync(int userId)
    {
        var candidate = await _candidateRepo.GetByUserIdAsync(userId);

        if (candidate == null || string.IsNullOrEmpty(candidate.ResumeFilePath))
            return null;

        if (!File.Exists(candidate.ResumeFilePath))
            return null;

        var bytes = await File.ReadAllBytesAsync(candidate.ResumeFilePath);
        var ext = Path.GetExtension(candidate.ResumeFilePath).ToLower();

        var contentType = ext == ".pdf"
            ? "application/pdf"
            : "application/vnd.openxmlformats-officedocument.wordprocessingml.document";

        return (bytes, contentType, candidate.ResumeOriginalName ?? "resume" + ext);
    }

    public async Task<bool> HasResumeAsync(int userId)
    {
        var candidate = await _candidateRepo.GetByUserIdAsync(userId);

        return candidate != null &&
               !string.IsNullOrEmpty(candidate.ResumeFilePath) &&
               File.Exists(candidate.ResumeFilePath);
    }

    public async Task<ApiResponse<bool>> DeleteResumeAsync(int userId)
    {
        var candidate = await _candidateRepo.GetByUserIdAsync(userId);

        if (candidate == null || string.IsNullOrEmpty(candidate.ResumeFilePath))
            return ApiResponse<bool>.Fail("No resume found");

        if (File.Exists(candidate.ResumeFilePath))
            File.Delete(candidate.ResumeFilePath);

        candidate.ResumeFilePath = null;
        candidate.ResumeOriginalName = null;

        await _candidateRepo.UpsertAsync(candidate);

        return ApiResponse<bool>.Ok(true);
    }

    //  HELPERS 

    private string ExtractTextFromPdf(string path)
    {
        using var doc = PdfDocument.Open(path);
        return string.Join("\n", doc.GetPages().Select(p => p.Text));
    }

    private string ExtractTextFromDocx(string path)
    {
        var text = new StringBuilder();

        using var zip = ZipFile.OpenRead(path);
        var entry = zip.GetEntry("word/document.xml");

        if (entry != null)
        {
            using var stream = entry.Open();
            var doc = XDocument.Load(stream);

            foreach (var node in doc.Descendants())
            {
                if (node.Name.LocalName == "t")
                    text.Append(node.Value);
                else if (node.Name.LocalName == "p")
                    text.AppendLine();
            }
        }

        return text.ToString().Trim();
    }

    private string NormalizeSkill(string skill)
    {
        var s = skill.Trim().ToLower();

        return s switch
        {
            ".net core" => "ASP.NET Core",
            "asp.net" => "ASP.NET",
            "sql" => "SQL Server",
            "js" => "JavaScript",
            _ => CultureInfo.CurrentCulture.TextInfo.ToTitleCase(s)
        };
    }
}