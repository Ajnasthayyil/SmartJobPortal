using MediatR;
using SmartJobPortal.Application.Common;
using SmartJobPortal.Application.DTOs.Candidate;
using SmartJobPortal.Application.Interfaces;
using SmartJobPortal.Domain.Entities;

namespace SmartJobPortal.Application.Features.Candidate.Queries.GetCandidateProfile;

public class GetCandidateProfileQueryHandler : IRequestHandler<GetCandidateProfileQuery, ApiResponse<CandidateProfileResponse>>
{
    private readonly ICandidateRepository _candidateRepo;
    private readonly IUserRepository _userRepo;
    private readonly ICacheService _cache;

    public GetCandidateProfileQueryHandler(
        ICandidateRepository candidateRepo,
        IUserRepository userRepo,
        ICacheService cache)
    {
        _candidateRepo = candidateRepo;
        _userRepo = userRepo;
        _cache = cache;
    }

    public async Task<ApiResponse<CandidateProfileResponse>> Handle(GetCandidateProfileQuery request, CancellationToken cancellationToken)
    {
        var userId = request.UserId;
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

    private static CandidateProfileResponse BuildResponse(
        SmartJobPortal.Domain.Entities.Candidate c, User u, List<CandidateSkill> skills,
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
            LinkedInUrl = c.LinkedInUrl,
            GitHubUrl = c.GitHubUrl,
            LeetCodeUrl = c.LeetCodeUrl,
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
                GraduationYear = e.GraduationYear,
                FieldOfStudy = e.FieldOfStudy
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
