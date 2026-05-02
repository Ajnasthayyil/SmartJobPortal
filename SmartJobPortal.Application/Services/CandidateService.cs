using SmartJobPortal.Application.Common;
using SmartJobPortal.Application.DTOs.Candidate;
using SmartJobPortal.Application.Interfaces;
using SmartJobPortal.Domain.Entities;

namespace SmartJobPortal.Application.Services;

public class CandidateService : ICandidateService
{
    private readonly ICandidateRepository _candidateRepo;
    private readonly IUserRepository _userRepo;
    private readonly ICacheService _cache;

    public CandidateService(
        ICandidateRepository candidateRepo,
        IUserRepository userRepo,
        ICacheService cache)
    {
        _candidateRepo = candidateRepo;
        _userRepo = userRepo;
        _cache = cache;
    }

    public async Task<ApiResponse<CandidateProfileResponse>> GetProfileAsync(int userId)
    {
        var cacheKey = $"candidate:profile:{userId}";

        var cached = await _cache.GetAsync<CandidateProfileResponse>(cacheKey);
        if (cached != null)
            return ApiResponse<CandidateProfileResponse>.Ok(cached);

        var user = await _userRepo.GetByIdAsync(userId);
        if (user == null)
            return ApiResponse<CandidateProfileResponse>.NotFound("User not found.");

        var candidate = await _candidateRepo.GetByUserIdAsync(userId);

        // Return shell if candidate hasn't filled profile yet
        if (candidate == null)
        {
            return ApiResponse<CandidateProfileResponse>.Ok(new CandidateProfileResponse
            {
                UserId = user.UserId,
                FullName = user.FullName,
                Email = user.Email
            });
        }

        var (skills, education, experience) = await _candidateRepo.GetFullProfileDataAsync(candidate.CandidateId);
        var response = BuildResponse(candidate, user, skills, education, experience);

        await _cache.SetAsync(cacheKey, response, TimeSpan.FromMinutes(30));
        return ApiResponse<CandidateProfileResponse>.Ok(response);
    }

    public async Task<ApiResponse<CandidateProfileResponse>> UpsertProfileAsync(
        int userId, CandidateProfileRequest request)
    {
        var user = await _userRepo.GetByIdAsync(userId);
        if (user == null)
            return ApiResponse<CandidateProfileResponse>.NotFound("User not found.");

        var existing = await _candidateRepo.GetByUserIdAsync(userId);
        var candidate = existing ?? new Candidate { UserId = userId };

        candidate.Headline = request.Headline;
        candidate.Summary = request.Summary;
        candidate.Location = request.Location;
        candidate.ExperienceYears = request.ExperienceYears;
        candidate.UpdatedAt = DateTime.Now;

        var upsertedId = await _candidateRepo.UpsertAsync(candidate);
        var candidateId = candidate.CandidateId > 0 ? candidate.CandidateId : upsertedId;
        candidate.CandidateId = candidateId;

        // Resolve skill names → IDs, create if new
        var skillRows = new List<CandidateSkill>();
        foreach (var s in request.Skills)
        {
            var name = s.SkillName.Trim();
            var skillId = await _candidateRepo.GetSkillIdByNameAsync(name)
                          ?? await _candidateRepo.CreateSkillAsync(name);

            skillRows.Add(new CandidateSkill
            {
                CandidateId = candidateId,
                SkillId = skillId,
                Level = s.Level
            });
        }

        // Atomic replace — DELETE old rows + INSERT new in one transaction
        await _candidateRepo.ReplaceSkillsAsync(candidateId, skillRows);

        // Bust cache
        await _cache.RemoveAsync($"candidate:profile:{userId}");
        await _cache.RemoveAsync($"candidate:skills:{candidateId}");

        var skills = await _candidateRepo.GetSkillsAsync(candidateId);
        var education = await _candidateRepo.GetEducationAsync(candidateId);
        var experience = await _candidateRepo.GetExperienceAsync(candidateId);
        var response = BuildResponse(candidate, user, skills, education, experience);
        await _cache.SetAsync($"candidate:profile:{userId}", response, TimeSpan.FromMinutes(30));

        return ApiResponse<CandidateProfileResponse>.Ok(response, "Profile updated successfully.");
    }

    private static CandidateProfileResponse BuildResponse(
        Candidate c, User u, List<CandidateSkill> skills,
        List<CandidateEducation> education,
        List<CandidateExperience> experience) => new()
        {
            CandidateId = c.CandidateId,
            UserId = u.UserId,
            FullName = u.FullName,
            Email = u.Email,
            PhoneNumber = u.PhoneNumber,
            Headline = c.Headline,
            Summary = c.Summary,
            Location = c.Location,
            ExperienceYears = c.ExperienceYears,
            HasResume = c.HasResume(),
            ResumeOriginalName = c.ResumeOriginalName,
            ResumeUploadedAt = c.ResumeUploadedAt,
            Skills = skills.Select(s => new SkillResponse
            {
                SkillId = s.SkillId,
                SkillName = s.SkillName,
                Level = s.Level,
                Category = s.Category
            }).ToList(),
            Education = education.Select(e => new EducationResponse
            {
                EducationId = e.EducationId,
                Degree = e.Degree,
                Institution = e.Institution,
                GraduationYear = e.GraduationYear
            }).ToList(),
            WorkExperience = experience.Select(e => new ExperienceResponse
            {
                ExperienceId = e.ExperienceId,
                Company = e.Company,
                Role = e.Role,
                Duration = e.Duration,
                Description = e.Description
            }).ToList()
        };
}