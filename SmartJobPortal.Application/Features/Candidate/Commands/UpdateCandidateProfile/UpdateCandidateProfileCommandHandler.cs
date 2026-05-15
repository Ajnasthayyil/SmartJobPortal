using MediatR;
using SmartJobPortal.Application.Common;
using SmartJobPortal.Application.DTOs.Candidate;
using SmartJobPortal.Application.Interfaces;
using SmartJobPortal.Domain.Entities;

namespace SmartJobPortal.Application.Features.Candidate.Commands.UpdateCandidateProfile;

public class UpdateCandidateProfileCommandHandler : IRequestHandler<UpdateCandidateProfileCommand, ApiResponse<CandidateProfileResponse>>
{
    private readonly ICandidateRepository _candidateRepo;
    private readonly IUserRepository _userRepo;
    private readonly ICacheService _cache;

    public UpdateCandidateProfileCommandHandler(
        ICandidateRepository candidateRepo,
        IUserRepository userRepo,
        ICacheService cache)
    {
        _candidateRepo = candidateRepo;
        _userRepo = userRepo;
        _cache = cache;
    }

    public async Task<ApiResponse<CandidateProfileResponse>> Handle(UpdateCandidateProfileCommand request, CancellationToken cancellationToken)
    {
        var userId = request.UserId;
        var profileRequest = request.Request;

        try
        {
            var user = await _userRepo.GetByIdAsync(userId);
            if (user == null)
                return ApiResponse<CandidateProfileResponse>.NotFound("User not found.");

            var existing = await _candidateRepo.GetByUserIdAsync(userId);
            var candidate = existing ?? new SmartJobPortal.Domain.Entities.Candidate { UserId = userId };

            candidate.Headline = profileRequest.Headline;
            candidate.Summary = profileRequest.Summary;
            candidate.Location = profileRequest.Location;
            candidate.ExperienceYears = profileRequest.ExperienceYears;
            candidate.LinkedInUrl = profileRequest.LinkedInUrl;
            candidate.GitHubUrl = profileRequest.GitHubUrl;
            candidate.LeetCodeUrl = profileRequest.LeetCodeUrl;
            candidate.UpdatedAt = DateTime.Now;

            // Update Phone Number if provided
            if (!string.IsNullOrWhiteSpace(profileRequest.PhoneNumber) && user.PhoneNumber != profileRequest.PhoneNumber)
            {
                await _userRepo.UpdatePhoneNumberAsync(userId, profileRequest.PhoneNumber);
                user.PhoneNumber = profileRequest.PhoneNumber;
            }

            var upsertedId = await _candidateRepo.UpsertAsync(candidate);
            var candidateId = candidate.CandidateId > 0 ? candidate.CandidateId : upsertedId;
            candidate.CandidateId = candidateId;

            // 1 — Skills
            var skillRows = new List<CandidateSkill>();
            foreach (var s in profileRequest.Skills)
            {
                var name = s.SkillName?.Trim();
                if (string.IsNullOrWhiteSpace(name)) continue;

                var skillId = await _candidateRepo.GetSkillIdByNameAsync(name)
                              ?? await _candidateRepo.CreateSkillAsync(name);

                skillRows.Add(new CandidateSkill
                {
                    CandidateId = candidateId,
                    SkillId = skillId,
                    Level = s.Level
                });
            }
            await _candidateRepo.ReplaceSkillsAsync(candidateId, skillRows);

            // 2 — Education & Experience (Clear and Replace)
            await _candidateRepo.ClearEducationAndExperienceAsync(candidateId);

            if (profileRequest.Education.Any())
            {
                var eduRows = profileRequest.Education.Select(e => new CandidateEducation
                {
                    CandidateId = candidateId,
                    Degree = e.Degree,
                    Institution = e.Institution,
                    GraduationYear = e.Duration // Frontend sends duration string
                }).ToList();
                await _candidateRepo.AddEducationAsync(eduRows);
            }

            if (profileRequest.WorkExperience.Any())
            {
                var expRows = profileRequest.WorkExperience.Select(e => new CandidateExperience
                {
                    CandidateId = candidateId,
                    Company = e.Company,
                    Role = e.Role,
                    Duration = e.Duration,
                    Description = e.Description
                }).ToList();
                await _candidateRepo.AddExperienceAsync(expRows);
            }

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
        catch (Exception ex)
        {
            return ApiResponse<CandidateProfileResponse>.Fail($"Update Failed: {ex.Message}", 500);
        }
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
