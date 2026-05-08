using SmartJobPortal.Application.Common;
using SmartJobPortal.Application.DTOs.Recruiter;
using SmartJobPortal.Application.Interfaces;
using SmartJobPortal.Domain.Entities;
using SmartJobPortal.Application.DTOs.NotificationDTOs;

namespace SmartJobPortal.Application.Services;

public class RecruiterService : IRecruiterService
{
    private readonly IRecruiterRepository _recruiterRepo;
    private readonly IRecruiterJobRepository _jobRepo;
    private readonly IUserRepository _userRepo;
    private readonly ICandidateRepository _candidateRepo;
    private readonly ICacheService _cache;
    private readonly INotificationService _notificationService;

    // Valid status transitions
    private static readonly Dictionary<string, List<string>> AllowedTransitions = new()
    {
        ["Applied"] = new() { "UnderReview", "Rejected" },
        ["UnderReview"] = new() { "Shortlisted", "Rejected" },
        ["Shortlisted"] = new() { "Interview", "Rejected" },
        ["Interview"] = new() { "Offered", "Rejected" },
        ["Offered"] = new() { "Rejected" },
        ["Rejected"] = new()
    };

    public RecruiterService(
        IRecruiterRepository recruiterRepo,
        IRecruiterJobRepository jobRepo,
        IUserRepository userRepo,
        ICandidateRepository candidateRepo,
        ICacheService cache,
        INotificationService notificationService)
    {
        _recruiterRepo = recruiterRepo;
        _jobRepo = jobRepo;
        _userRepo = userRepo;
        _candidateRepo = candidateRepo;
        _cache = cache;
        _notificationService = notificationService;
    }

    // ── Profile ────────────────────────────────────────────────────

    public async Task<ApiResponse<RecruiterProfileResponse>> GetProfileAsync(int userId)
    {
        var cacheKey = $"recruiter:profile:{userId}";
        var cached = await _cache.GetAsync<RecruiterProfileResponse>(cacheKey);
        if (cached != null) return ApiResponse<RecruiterProfileResponse>.Ok(cached);

        var user = await _userRepo.GetByIdAsync(userId);
        if (user == null) return ApiResponse<RecruiterProfileResponse>.NotFound("User not found.");

        var recruiter = await _recruiterRepo.GetByUserIdAsync(userId);
        if (recruiter == null)
        {
            return ApiResponse<RecruiterProfileResponse>.Ok(new RecruiterProfileResponse
            {
                UserId = user.UserId,
                FullName = user.FullName,
                Email = user.Email
            });
        }

        var totalJobs = await _recruiterRepo.GetTotalJobsPostedAsync(recruiter.RecruiterId);
        var response = BuildProfileResponse(recruiter, user, totalJobs);
        await _cache.SetAsync(cacheKey, response, TimeSpan.FromMinutes(30));
        return ApiResponse<RecruiterProfileResponse>.Ok(response);
    }

    public async Task<ApiResponse<RecruiterProfileResponse>> UpsertProfileAsync(int userId, RecruiterProfileRequest request)
    {
        try
        {
            var user = await _userRepo.GetByIdAsync(userId);
            if (user == null) return ApiResponse<RecruiterProfileResponse>.NotFound("User not found.");

            var existing = await _recruiterRepo.GetByUserIdAsync(userId);
            var recruiter = existing ?? new Recruiter { UserId = userId };

            recruiter.CompanyName = request.CompanyName;
            recruiter.Website = request.Website;
            recruiter.Industry = request.Industry;
            recruiter.Description = request.Description;
            recruiter.Location = request.Location;
            recruiter.UpdatedAt = DateTime.Now;

            var recruiterId = await _recruiterRepo.UpsertAsync(recruiter);
            recruiter.RecruiterId = recruiterId;

            await _cache.RemoveAsync($"recruiter:profile:{userId}");
            var totalJobs = await _recruiterRepo.GetTotalJobsPostedAsync(recruiterId);
            return ApiResponse<RecruiterProfileResponse>.Ok(BuildProfileResponse(recruiter, user, totalJobs));
        }
        catch (Exception ex)
        {
            return ApiResponse<RecruiterProfileResponse>.Fail($"Database Error: {ex.Message}", 500);
        }
    }

    // ── Jobs ───────────────────────────────────────────────────────

    public async Task<ApiResponse<JobResponse>> PostJobAsync(int userId, PostJobRequest request)
    {
        try
        {
            var recruiter = await _recruiterRepo.GetByUserIdAsync(userId);
            if (recruiter == null || !recruiter.IsApproved)
                return ApiResponse<JobResponse>.Fail("Only approved recruiters can post jobs.", 403);

            var job = new Job
            {
                RecruiterId = recruiter.RecruiterId,
                Title = request.Title,
                Description = request.Description,
                Location = request.Location,
                JobType = request.JobType,
                MinSalary = request.MinSalary,
                MaxSalary = request.MaxSalary,
                MinExperienceYears = request.MinExperienceYears,
                IsActive = true,
                PostedAt = DateTime.Now,
                ExpiresAt = DateTime.Now.AddDays(30),
                UpdatedAt = DateTime.Now
            };

            var jobId = await _jobRepo.CreateJobAsync(job);
            job.JobId = jobId;

            // ── SAVE SKILLS ──
            var skillIds = new List<int>();
            if (request.RequiredSkills != null && request.RequiredSkills.Any())
            {
                foreach (var s in request.RequiredSkills)
                {
                    var name = s.Trim();
                    if (string.IsNullOrEmpty(name)) continue;

                    var skillId = await _candidateRepo.GetSkillIdByNameAsync(name)
                                  ?? await _candidateRepo.CreateSkillAsync(name);
                    skillIds.Add(skillId);
                }

                if (skillIds.Any())
                {
                    await _jobRepo.ReplaceJobSkillsAsync(jobId, skillIds);
                }
            }

            var finalSkills = await _jobRepo.GetJobSkillsAsync(jobId);
            return ApiResponse<JobResponse>.Ok(await BuildJobResponseAsync(job, finalSkills, 0), "Job posted successfully.");
        }
        catch (Exception ex)
        {
            return ApiResponse<JobResponse>.Fail($"Failed to post job: {ex.Message}", 500);
        }
    }

    public async Task<ApiResponse<List<JobResponse>>> GetMyJobsAsync(int userId)
    {
        var recruiter = await _recruiterRepo.GetByUserIdAsync(userId);
        if (recruiter == null) return ApiResponse<List<JobResponse>>.Ok(new());

        var jobs = await _jobRepo.GetJobsByRecruiterIdAsync(recruiter.RecruiterId);
        return ApiResponse<List<JobResponse>>.Ok(jobs);
    }

    public async Task<ApiResponse<JobResponse>> GetJobDetailAsync(int userId, int jobId)
    {
        var recruiter = await _recruiterRepo.GetByUserIdAsync(userId);
        if (recruiter == null) return ApiResponse<JobResponse>.Fail("Access denied.", 403);

        var job = await _jobRepo.GetJobByIdAsync(jobId);
        if (job == null || job.RecruiterId != recruiter.RecruiterId)
            return ApiResponse<JobResponse>.NotFound("Job not found.");

        var skills = await _jobRepo.GetJobSkillsAsync(jobId);
        var totalApps = await _jobRepo.GetTotalApplicantsAsync(jobId);
        return ApiResponse<JobResponse>.Ok(await BuildJobResponseAsync(job, skills, totalApps));
    }

    public async Task<ApiResponse<JobResponse>> UpdateJobAsync(int userId, int jobId, UpdateJobRequest request)
    {
        try
        {
            var recruiter = await _recruiterRepo.GetByUserIdAsync(userId);
            if (recruiter == null) return ApiResponse<JobResponse>.Fail("Access denied.", 403);

            var job = await _jobRepo.GetJobByIdAsync(jobId);
            if (job == null || job.RecruiterId != recruiter.RecruiterId)
                return ApiResponse<JobResponse>.NotFound("Job not found.");

            job.Title = request.Title;
            job.Description = request.Description;
            job.Location = request.Location;
            job.JobType = request.JobType;
            job.MinSalary = request.MinSalary;
            job.MaxSalary = request.MaxSalary;
            job.MinExperienceYears = request.MinExperienceYears;
            job.IsActive = request.IsActive;
            job.UpdatedAt = DateTime.Now;

            await _jobRepo.UpdateJobAsync(job);

            // Bust cache
            await _cache.RemoveAsync($"job:{jobId}");

            // ── UPDATE SKILLS ──
            if (request.RequiredSkills != null)
            {
                var skillIds = new List<int>();
                foreach (var s in request.RequiredSkills)
                {
                    var name = s.Trim();
                    if (string.IsNullOrEmpty(name)) continue;

                    var skillId = await _candidateRepo.GetSkillIdByNameAsync(name)
                                  ?? await _candidateRepo.CreateSkillAsync(name);
                    skillIds.Add(skillId);
                }
                await _jobRepo.ReplaceJobSkillsAsync(jobId, skillIds);
            }

            var finalSkills = await _jobRepo.GetJobSkillsAsync(jobId);
            return ApiResponse<JobResponse>.Ok(await BuildJobResponseAsync(job, finalSkills, 0), "Job updated successfully.");
        }
        catch (Exception ex)
        {
            return ApiResponse<JobResponse>.Fail($"Failed to update job: {ex.Message}", 500);
        }
    }

    public async Task<ApiResponse<string>> DeleteJobAsync(int userId, int jobId)
    {
        var recruiter = await _recruiterRepo.GetByUserIdAsync(userId);
        if (recruiter == null) return ApiResponse<string>.Fail("Access denied.", 403);

        var deleted = await _jobRepo.SoftDeleteJobAsync(jobId, recruiter.RecruiterId);
        if (!deleted) return ApiResponse<string>.NotFound("Job not found.");

        // Bust cache
        await _cache.RemoveAsync($"job:{jobId}");

        return ApiResponse<string>.Ok("Job deleted successfully.");
    }

    public async Task<ApiResponse<string>> ToggleJobStatusAsync(int userId, int jobId)
    {
        var recruiter = await _recruiterRepo.GetByUserIdAsync(userId);
        if (recruiter == null) return ApiResponse<string>.Fail("Access denied.", 403);

        var toggled = await _jobRepo.ToggleJobStatusAsync(jobId, recruiter.RecruiterId);
        if (!toggled) return ApiResponse<string>.NotFound("Job not found.");

        // Bust cache
        await _cache.RemoveAsync($"job:{jobId}");

        return ApiResponse<string>.Ok("Status toggled successfully.");
    }

    // ── Applicants ───────────────────────────────────────────────

    public async Task<ApiResponse<List<ApplicantResponse>>> GetApplicantsAsync(int userId, int jobId)
    {
        var recruiter = await _recruiterRepo.GetByUserIdAsync(userId);
        if (recruiter == null) return ApiResponse<List<ApplicantResponse>>.Fail("Access denied.", 403);

        if (!await _jobRepo.JobBelongsToRecruiterAsync(jobId, recruiter.RecruiterId))
            return ApiResponse<List<ApplicantResponse>>.NotFound("Job not found.");

        var apps = await _jobRepo.GetApplicantsAsync(jobId);
        return ApiResponse<List<ApplicantResponse>>.Ok(apps);
    }

    public async Task<ApiResponse<List<ApplicantResponse>>> GetRankedApplicantsAsync(int userId, int jobId)
    {
        // Implementation of ranking logic would go here
        return await GetApplicantsAsync(userId, jobId);
    }

    public async Task<ApiResponse<string>> UpdateApplicationStatusAsync(int userId, int applicationId, UpdateStatusRequest request)
    {
        var recruiter = await _recruiterRepo.GetByUserIdAsync(userId);
        if (recruiter == null) return ApiResponse<string>.Fail("Access denied.", 403);

        var current = await _jobRepo.GetApplicationStatusAsync(applicationId);
        if (string.IsNullOrEmpty(current)) return ApiResponse<string>.NotFound("Application not found.");

        if (!AllowedTransitions.ContainsKey(current) || !AllowedTransitions[current].Contains(request.Status))
            return ApiResponse<string>.Fail($"Invalid transition from {current} to {request.Status}.", 400);

        var updated = await _jobRepo.UpdateApplicationStatusAsync(applicationId, recruiter.RecruiterId, request.Status);
        if (!updated) return ApiResponse<string>.Fail("Failed to update status.", 500);

        // Notify candidate
        var details = await _jobRepo.GetApplicationDetailsAsync(applicationId);
        if (details.HasValue)
        {
            var (title, message, type) = NotificationTemplates.ApplicationStatusChanged(
                request.Status, details.Value.JobTitle, details.Value.CompanyName);
            await _notificationService.CreateAsync(details.Value.CandidateUserId, title, message, type, details.Value.JobTitle, details.Value.CompanyName);
        }

        return ApiResponse<string>.Ok($"Status updated to {request.Status}.");
    }

    // ── Helpers ──────────────────────────────────────────────────

    private RecruiterProfileResponse BuildProfileResponse(Recruiter r, User u, int totalJobs) =>
        new()
        {
            RecruiterId = r.RecruiterId,
            UserId = u.UserId,
            FullName = u.FullName,
            Email = u.Email,
            CompanyName = r.CompanyName,
            Website = r.Website,
            Industry = r.Industry,
            Description = r.Description,
            Location = r.Location,
            CreatedAt = r.CreatedAt,
            TotalJobsPosted = totalJobs
        };

    private async Task<JobResponse> BuildJobResponseAsync(Job job, List<string> skills, int totalApps) =>
        new()
        {
            JobId = job.JobId,
            RecruiterId = job.RecruiterId,
            Title = job.Title,
            Description = job.Description,
            Location = job.Location,
            JobType = job.JobType,
            MinSalary = job.MinSalary,
            MaxSalary = job.MaxSalary,
            MinExperienceYears = job.MinExperienceYears,
            IsActive = job.IsActive,
            PostedAt = job.PostedAt,
            ExpiresAt = job.ExpiresAt,
            RequiredSkills = skills,
            TotalApplicants = totalApps
        };
}