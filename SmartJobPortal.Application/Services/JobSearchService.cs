using SmartJobPortal.Application.Common;
using SmartJobPortal.Application.DTOs.Candidate;
using SmartJobPortal.Application.Interfaces;
using SmartJobPortal.Domain.Entities;

namespace SmartJobPortal.Application.Services;

public class JobSearchService : IJobSearchService
{
    private readonly IJobRepository _jobRepo;
    private readonly ICandidateRepository _candidateRepo;
    private readonly IApplicationRepository _appRepo;
    private readonly IMatchScoreService _matchScoreService;
    private readonly ICacheService _cache;

    public JobSearchService(
        IJobRepository jobRepo,
        ICandidateRepository candidateRepo,
        IApplicationRepository appRepo,
        IMatchScoreService matchScoreService,
        ICacheService cache)
    {
        _jobRepo = jobRepo;
        _candidateRepo = candidateRepo;
        _appRepo = appRepo;
        _matchScoreService = matchScoreService;
        _cache = cache;
    }

    public async Task<ApiResponse<JobSearchResponse>> SearchAsync(
        int userId, JobSearchRequest request)
    {
        var cacheKey = $"jobsearch:{request.ToCacheKey()}";
        var cached = await _cache.GetAsync<JobSearchResponse>(cacheKey);

        JobSearchResponse response;

        if (cached != null)
        {
            response = cached;
        }
        else
        {
            var (jobs, total) = await _jobRepo.SearchAsync(request);
            response = new JobSearchResponse
            {
                TotalCount = total,
                Page = request.Page,
                PageSize = request.PageSize,
                Jobs = jobs
            };
            // Cache results without scores (scores are per-user)
            await _cache.SetAsync(cacheKey, response, TimeSpan.FromMinutes(5));
        }

        // Attach per-candidate match scores
        if (userId > 0)
            await AttachScoresAsync(userId, response.Jobs);

        return ApiResponse<JobSearchResponse>.Ok(response);
    }

    public async Task<ApiResponse<JobDetail>> GetDetailAsync(int userId, int jobId)
    {
        var cacheKey = $"job:{jobId}";
        var job = await _cache.GetAsync<JobDetail>(cacheKey)
                  ?? await _jobRepo.GetDetailAsync(jobId);

        if (job == null)
            return ApiResponse<JobDetail>.NotFound("Job not found.");

        await _cache.SetAsync(cacheKey, job, TimeSpan.FromMinutes(30));

        // Attach match score for this candidate
        if (userId > 0)
        {
            var scoreResult = await _matchScoreService.GetOrCalculateAsync(userId, jobId);
            if (scoreResult.Success)
                job.MatchScore = scoreResult.Data;
        }

        return ApiResponse<JobDetail>.Ok(job);
    }

    public async Task<ApiResponse<int>> ApplyAsync(int userId, ApplyJobRequest request)
    {
        var candidate = await _candidateRepo.GetByUserIdAsync(userId);
        if (candidate == null)
            return ApiResponse<int>.Fail("Complete your profile before applying.");

        if (!candidate.HasResume())
            return ApiResponse<int>.Fail("Upload a resume before applying.");

        if (await _appRepo.AlreadyAppliedAsync(candidate.CandidateId, request.JobId))
            return ApiResponse<int>.Fail("You have already applied to this job.");

        var job = await _jobRepo.GetDetailAsync(request.JobId);
        if (job == null)
            return ApiResponse<int>.NotFound("Job not found.");

        var applicationId = await _appRepo.CreateAsync(new SmartJobPortal.Domain.Entities.Application
        {
            CandidateId = candidate.CandidateId,
            JobId = request.JobId,
            CoverNote = request.CoverNote,
            Status = "Applied",
            AppliedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        });

        // Pre-calculate score so it's ready when candidate views applications
        await _matchScoreService.GetOrCalculateAsync(userId, request.JobId);

        return ApiResponse<int>.Ok(applicationId, "Application submitted successfully.");
    }

    public async Task<ApiResponse<List<ApplicationTrackingResponse>>> GetMyApplicationsAsync(
        int userId)
    {
        var candidate = await _candidateRepo.GetByUserIdAsync(userId);
        if (candidate == null)
            return ApiResponse<List<ApplicationTrackingResponse>>.Ok(new());

        var applications = await _appRepo.GetByCandidateIdAsync(candidate.CandidateId);

        // Build status timeline for each application
        var allStatuses = new[]
        {
            "Applied", "UnderReview", "Shortlisted", "Interview", "Offered"
        };

        foreach (var app in applications)
        {
            var currentIndex = Array.IndexOf(allStatuses, app.Status);
            app.Timeline = allStatuses.Select((status, i) => new StatusTimelineItem
            {
                Status = status,
                IsCompleted = i < currentIndex,
                IsCurrent = i == currentIndex,
                OccurredAt = i <= currentIndex ? app.AppliedAt.AddDays(i) : null
            }).ToList();

            // Attach match score
            var scoreResult = await _matchScoreService.GetOrCalculateAsync(userId, app.JobId);
            if (scoreResult.Success)
                app.MatchScore = scoreResult.Data?.TotalScore;
        }

        return ApiResponse<List<ApplicationTrackingResponse>>.Ok(applications);
    }

    private async Task AttachScoresAsync(int userId, List<JobListItem> jobs)
    {
        var jobIds = jobs.Select(j => j.JobId).ToList();
        var results = await _matchScoreService.GetBulkAsync(userId, jobIds);

        if (!results.Success || results.Data == null) return;

        var scoreMap = results.Data.ToDictionary(s => s.JobId);
        foreach (var job in jobs)
        {
            if (scoreMap.TryGetValue(job.JobId, out var s))
                job.MatchScore = s.TotalScore;
        }
    }

    public async Task<List<JobListItem>> RecommendJobsBySkillsAsync(int userId, List<string> skills)
    {
        var jobs = await _jobRepo.GetBySkillsAsync(skills);
        
        if (userId > 0 && jobs.Any())
            await AttachScoresAsync(userId, jobs);

        return jobs.OrderByDescending(j => j.MatchScore).ToList();
    }
}