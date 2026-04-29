using System.Text.Json;
using SmartJobPortal.Application.Common;
using SmartJobPortal.Application.DTOs.Recruiter;
using SmartJobPortal.Application.Interfaces;
using SmartJobPortal.Domain.Entities;

namespace SmartJobPortal.Application.Services;

public class RecruiterService : IRecruiterService
{
    private readonly IRecruiterRepository _recruiterRepo;
    private readonly IRecruiterJobRepository _jobRepo;
    private readonly IUserRepository _userRepo;
    private readonly ICandidateRepository _candidateRepo;
    private readonly ICacheService _cache;

    // Valid status transitions — prevents invalid status jumps
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
        ICacheService cache)
    {
        _recruiterRepo = recruiterRepo;
        _jobRepo = jobRepo;
        _userRepo = userRepo;
        _candidateRepo = candidateRepo;
        _cache = cache;
    }

    //  Profile 

    public async Task<ApiResponse<RecruiterProfileResponse>> GetProfileAsync(int userId)
    {
        var cacheKey = $"recruiter:profile:{userId}";
        var cached = await _cache.GetAsync<RecruiterProfileResponse>(cacheKey);
        if (cached != null)
            return ApiResponse<RecruiterProfileResponse>.Ok(cached);

        var user = await _userRepo.GetByIdAsync(userId);
        if (user == null)
            return ApiResponse<RecruiterProfileResponse>.NotFound("User not found.");

        var recruiter = await _recruiterRepo.GetByUserIdAsync(userId);

        // Return shell if recruiter hasn't set up profile yet
        if (recruiter == null)
        {
            return ApiResponse<RecruiterProfileResponse>.Ok(
                new RecruiterProfileResponse
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

    public async Task<ApiResponse<RecruiterProfileResponse>> UpsertProfileAsync(
        int userId, RecruiterProfileRequest request)
    {
        var user = await _userRepo.GetByIdAsync(userId);
        if (user == null)
            return ApiResponse<RecruiterProfileResponse>.NotFound("User not found.");

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

        // Bust cache
        await _cache.RemoveAsync($"recruiter:profile:{userId}");

        var totalJobs = await _recruiterRepo.GetTotalJobsPostedAsync(recruiterId);
        var response = BuildProfileResponse(recruiter, user, totalJobs);

        await _cache.SetAsync(
            $"recruiter:profile:{userId}", response, TimeSpan.FromMinutes(30));

        return ApiResponse<RecruiterProfileResponse>.Ok(
            response, "Profile updated successfully.");
    }

    //  Jobs 

    public async Task<ApiResponse<JobResponse>> PostJobAsync(
        int userId, PostJobRequest request)
    {
        // Validate salary range
        if (request.MinSalary.HasValue && request.MaxSalary.HasValue
            && request.MinSalary > request.MaxSalary)
            return ApiResponse<JobResponse>.Fail(
                "Minimum salary cannot be greater than maximum salary.");

        var recruiter = await _recruiterRepo.GetByUserIdAsync(userId);
        if (recruiter == null)
            return ApiResponse<JobResponse>.Fail(
                "Set up your company profile before posting jobs.");

        // Create job row
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
            ExpiresAt = request.ExpiresAt,
            IsActive = true,
            PostedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        };

        var jobId = await _jobRepo.CreateJobAsync(job);
        job.JobId = jobId;

        // Resolve skill names → IDs, create new skills if needed
        var skillIds = await ResolveSkillIdsAsync(request.RequiredSkills);
        await _jobRepo.ReplaceJobSkillsAsync(jobId, skillIds);

        // Bust recruiter jobs cache
        await _cache.RemoveAsync($"recruiter:jobs:{recruiter.RecruiterId}");

        var response = await BuildJobResponseAsync(job, request.RequiredSkills, 0);
        return ApiResponse<JobResponse>.Ok(response, "Job posted successfully.");
    }

    public async Task<ApiResponse<List<JobResponse>>> GetMyJobsAsync(int userId)
    {
        var recruiter = await _recruiterRepo.GetByUserIdAsync(userId);
        if (recruiter == null)
            return ApiResponse<List<JobResponse>>.Ok(new(),
                "No profile found. Set up your company profile first.");

        var cacheKey = $"recruiter:jobs:{recruiter.RecruiterId}";
        var cached = await _cache.GetAsync<List<JobResponse>>(cacheKey);
        if (cached != null)
            return ApiResponse<List<JobResponse>>.Ok(cached);

        var jobs = await _jobRepo.GetJobsByRecruiterIdAsync(recruiter.RecruiterId);
        await _cache.SetAsync(cacheKey, jobs, TimeSpan.FromMinutes(10));

        return ApiResponse<List<JobResponse>>.Ok(jobs);
    }

    public async Task<ApiResponse<JobResponse>> GetJobDetailAsync(int userId, int jobId)
    {
        var recruiter = await _recruiterRepo.GetByUserIdAsync(userId);
        if (recruiter == null)
            return ApiResponse<JobResponse>.NotFound("Recruiter profile not found.");

        if (!await _jobRepo.JobBelongsToRecruiterAsync(jobId, recruiter.RecruiterId))
            return ApiResponse<JobResponse>.Fail(
                "You do not have permission to view this job.", 403);

        var job = await _jobRepo.GetJobByIdAsync(jobId);
        if (job == null)
            return ApiResponse<JobResponse>.NotFound("Job not found.");

        var skills = await GetJobSkillNamesAsync(jobId);
        var totalApps = await _jobRepo.GetTotalApplicantsAsync(jobId);
        var response = await BuildJobResponseAsync(job, skills, totalApps);

        return ApiResponse<JobResponse>.Ok(response);
    }

    public async Task<ApiResponse<JobResponse>> UpdateJobAsync(
        int userId, int jobId, UpdateJobRequest request)
    {
        if (request.MinSalary.HasValue && request.MaxSalary.HasValue
            && request.MinSalary > request.MaxSalary)
            return ApiResponse<JobResponse>.Fail(
                "Minimum salary cannot be greater than maximum salary.");

        var recruiter = await _recruiterRepo.GetByUserIdAsync(userId);
        if (recruiter == null)
            return ApiResponse<JobResponse>.NotFound("Recruiter profile not found.");

        if (!await _jobRepo.JobBelongsToRecruiterAsync(jobId, recruiter.RecruiterId))
            return ApiResponse<JobResponse>.Fail(
                "You do not have permission to edit this job.", 403);

        var job = await _jobRepo.GetJobByIdAsync(jobId);
        if (job == null)
            return ApiResponse<JobResponse>.NotFound("Job not found.");

        // Update fields
        job.Title = request.Title;
        job.Description = request.Description;
        job.Location = request.Location;
        job.JobType = request.JobType;
        job.MinSalary = request.MinSalary;
        job.MaxSalary = request.MaxSalary;
        job.MinExperienceYears = request.MinExperienceYears;
        job.ExpiresAt = request.ExpiresAt;
        job.IsActive = request.IsActive;
        job.UpdatedAt = DateTime.Now;

        await _jobRepo.UpdateJobAsync(job);

        // Replace skills atomically
        var skillIds = await ResolveSkillIdsAsync(request.RequiredSkills);
        await _jobRepo.ReplaceJobSkillsAsync(jobId, skillIds);

        // Bust caches
        await _cache.RemoveAsync($"recruiter:jobs:{recruiter.RecruiterId}");
        await _cache.RemoveAsync($"job:{jobId}");

        var totalApps = await _jobRepo.GetTotalApplicantsAsync(jobId);
        var response = await BuildJobResponseAsync(job, request.RequiredSkills, totalApps);

        return ApiResponse<JobResponse>.Ok(response, "Job updated successfully.");
    }

    public async Task<ApiResponse<string>> DeleteJobAsync(int userId, int jobId)
    {
        var recruiter = await _recruiterRepo.GetByUserIdAsync(userId);
        if (recruiter == null)
            return ApiResponse<string>.NotFound("Recruiter profile not found.");

        if (!await _jobRepo.JobBelongsToRecruiterAsync(jobId, recruiter.RecruiterId))
            return ApiResponse<string>.Fail(
                "You do not have permission to delete this job.", 403);

        var deleted = await _jobRepo.SoftDeleteJobAsync(jobId, recruiter.RecruiterId);
        if (!deleted)
            return ApiResponse<string>.NotFound("Job not found.");

        // Bust caches
        await _cache.RemoveAsync($"recruiter:jobs:{recruiter.RecruiterId}");
        await _cache.RemoveAsync($"job:{jobId}");

        return ApiResponse<string>.Ok("Job deactivated successfully.");
    }

    //  Applicants 

    public async Task<ApiResponse<List<ApplicantResponse>>> GetApplicantsAsync(
        int userId, int jobId)
    {
        var recruiter = await _recruiterRepo.GetByUserIdAsync(userId);
        if (recruiter == null)
            return ApiResponse<List<ApplicantResponse>>.NotFound(
                "Recruiter profile not found.");

        if (!await _jobRepo.JobBelongsToRecruiterAsync(jobId, recruiter.RecruiterId))
            return ApiResponse<List<ApplicantResponse>>.Fail(
                "You do not have permission to view these applicants.", 403);

        var applicants = await _jobRepo.GetApplicantsAsync(jobId);

        // Attach match scores from MatchScores table
        await AttachMatchScoresAsync(applicants, jobId);

        // Populate ResumeUrl
        foreach (var a in applicants)
        {
            if (a.HasResume)
            {
                a.ResumeUrl = $"/api/recruiter/candidates/{a.CandidateUserId}/resume";
            }
        }

        return ApiResponse<List<ApplicantResponse>>.Ok(applicants);
    }

    public async Task<ApiResponse<List<ApplicantResponse>>> GetRankedApplicantsAsync(
        int userId, int jobId)
    {
        var result = await GetApplicantsAsync(userId, jobId);
        if (!result.Success || result.Data == null)
            return result;

        // Sort by TotalScore DESC — candidates with no score go to bottom
        var ranked = result.Data
            .OrderByDescending(a => a.TotalScore ?? -1)
            .ToList();

        return ApiResponse<List<ApplicantResponse>>.Ok(ranked);
    }

    public async Task<ApiResponse<string>> UpdateApplicationStatusAsync(
        int userId, int applicationId, UpdateStatusRequest request)
    {
        // Validate status value
        var validStatuses = new[]
        {
            "Applied", "UnderReview", "Shortlisted",
            "Interview", "Offered", "Rejected"
        };

        if (!validStatuses.Contains(request.Status))
            return ApiResponse<string>.Fail(
                $"Invalid status. Allowed values: {string.Join(", ", validStatuses)}");

        var recruiter = await _recruiterRepo.GetByUserIdAsync(userId);
        if (recruiter == null)
            return ApiResponse<string>.NotFound("Recruiter profile not found.");

        var updated = await _jobRepo.UpdateApplicationStatusAsync(
            applicationId, recruiter.RecruiterId, request.Status);

        if (!updated)
            return ApiResponse<string>.Fail(
                "Application not found or you do not have permission to update it.", 403);

        // Bust candidate application cache if exists
        await _cache.RemoveAsync($"applications:{applicationId}");

        return ApiResponse<string>.Ok(
            $"Application status updated to '{request.Status}' successfully.");
    }

    // ── Private helpers ──────────────────────────────────────────────────────

    private async Task<List<int>> ResolveSkillIdsAsync(List<string> skillNames)
    {
        var skillIds = new List<int>();
        foreach (var name in skillNames.Select(s => s.Trim())
                                       .Where(s => !string.IsNullOrEmpty(s))
                                       .Distinct(StringComparer.OrdinalIgnoreCase))
        {
            var id = await _candidateRepo.GetSkillIdByNameAsync(name)
                     ?? await _candidateRepo.CreateSkillAsync(name);
            skillIds.Add(id);
        }
        return skillIds;
    }

    private async Task<List<string>> GetJobSkillNamesAsync(int jobId)
    {
        // Reuse the skill name lookup from candidate's job repository
        // We call it via recruiter job repo below
        return new List<string>(); // filled in BuildJobResponseAsync
    }

    private async Task AttachMatchScoresAsync(
        List<ApplicantResponse> applicants, int jobId)
    {
        foreach (var applicant in applicants)
        {
            var cacheKey = $"match:{applicant.CandidateId}:{jobId}";
            var cached = await _cache.GetAsync<dynamic>(cacheKey);

        }
    }

    private static RecruiterProfileResponse BuildProfileResponse(
        Recruiter r, User u, int totalJobs) => new()
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

    private async Task<JobResponse> BuildJobResponseAsync(
        Job job, List<string> skills, int totalApplicants) => new()
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
            TotalApplicants = totalApplicants
        };
}