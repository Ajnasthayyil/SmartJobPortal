using System.Text.Json;
using SmartJobPortal.Application.Common;
using SmartJobPortal.Application.DTOs.Candidate;
using SmartJobPortal.Application.Interfaces;
using SmartJobPortal.Domain.Entities;

namespace SmartJobPortal.Application.Services;

public class MatchScoreService : IMatchScoreService
{
    private readonly ICandidateRepository _candidateRepo;
    private readonly IJobRepository _jobRepo;
    private readonly IMatchScoreRepository _matchScoreRepo;
    private readonly ICacheService _cache;

    public MatchScoreService(
        ICandidateRepository candidateRepo,
        IJobRepository jobRepo,
        IMatchScoreRepository matchScoreRepo,
        ICacheService cache)
    {
        _candidateRepo = candidateRepo;
        _jobRepo = jobRepo;
        _matchScoreRepo = matchScoreRepo;
        _cache = cache;
    }

    public async Task<ApiResponse<MatchScoreResponse>> GetOrCalculateAsync(int userId, int jobId)
    {
        var candidate = await _candidateRepo.GetByUserIdAsync(userId);
        if (candidate == null)
            return ApiResponse<MatchScoreResponse>.Fail(
                "Complete your profile to see match scores.");

        var cacheKey = $"match:{candidate.CandidateId}:{jobId}";
        var cached = await _cache.GetAsync<MatchScoreResponse>(cacheKey);
        if (cached != null)
            return ApiResponse<MatchScoreResponse>.Ok(cached);

        var score = await CalculateAsync(candidate, jobId);
        if (score == null)
            return ApiResponse<MatchScoreResponse>.NotFound("Job not found.");

        await _matchScoreRepo.UpsertAsync(score);

        var response = await BuildResponseAsync(score, candidate.CandidateId, jobId);
        await _cache.SetAsync(cacheKey, response, TimeSpan.FromHours(1));

        return ApiResponse<MatchScoreResponse>.Ok(response);
    }

    public async Task<ApiResponse<List<MatchScoreResponse>>> GetBulkAsync(
        int userId, List<int> jobIds)
    {
        if (jobIds == null || !jobIds.Any())
            return ApiResponse<List<MatchScoreResponse>>.Ok(new());

        var candidate = await _candidateRepo.GetByUserIdAsync(userId);
        if (candidate == null)
            return ApiResponse<List<MatchScoreResponse>>.Ok(new());

        var results = new List<MatchScoreResponse>();
        var candidateSkills = (await _candidateRepo.GetSkillsAsync(candidate.CandidateId))
            .Select(s => s.SkillName.ToLowerInvariant())
            .ToHashSet();

        // 1. Check cache first for all IDs
        var missingJobIds = new List<int>();
        foreach (var jobId in jobIds)
        {
            var cacheKey = $"match:{candidate.CandidateId}:{jobId}";
            var cached = await _cache.GetAsync<MatchScoreResponse>(cacheKey);
            if (cached != null)
                results.Add(cached);
            else
                missingJobIds.Add(jobId);
        }

        if (!missingJobIds.Any())
            return ApiResponse<List<MatchScoreResponse>>.Ok(results);

        // 2. Bulk fetch metadata for missing ones
        var jobDetails = await _jobRepo.GetDetailsAsync(missingJobIds);
        var jobMap = jobDetails.ToDictionary(j => j.JobId);

        foreach (var jobId in missingJobIds)
        {
            if (!jobMap.TryGetValue(jobId, out var job)) continue;

            var score = CalculateInMemory(candidate, candidateSkills, job);
            await _matchScoreRepo.UpsertAsync(score);
            
            var response = BuildResponseInMemory(score, candidateSkills, job);
            await _cache.SetAsync($"match:{candidate.CandidateId}:{jobId}", response, TimeSpan.FromHours(1));
            results.Add(response);
        }

        return ApiResponse<List<MatchScoreResponse>>.Ok(results);
    }

    private MatchScore CalculateInMemory(Candidate candidate, HashSet<string> candidateSkills, JobDetail job)
    {
        var jobSkillsNorm = job.RequiredSkills.Select(s => s.ToLowerInvariant()).ToList();
        var matched = jobSkillsNorm.Where(s => candidateSkills.Contains(s)).ToList();
        var missing = jobSkillsNorm.Except(matched).ToList();

        var skillScore = jobSkillsNorm.Count > 0
            ? Math.Round((decimal)matched.Count / jobSkillsNorm.Count * 100, 2)
            : 0m;

        var expScore = job.MinExperienceYears == 0 ? 100m
            : Math.Min(Math.Round((decimal)candidate.ExperienceYears / job.MinExperienceYears * 100, 2), 100m);

        var locationScore = string.Equals(candidate.Location?.Trim(), job.Location?.Trim(), StringComparison.OrdinalIgnoreCase) ? 100m : 0m;

        return new MatchScore
        {
            CandidateId = candidate.CandidateId,
            JobId = job.JobId,
            SkillScore = skillScore,
            ExperienceScore = expScore,
            LocationScore = locationScore,
            TotalScore = Math.Round((skillScore * 0.6m) + (expScore * 0.3m) + (locationScore * 0.1m), 2),
            MissingSkills = JsonSerializer.Serialize(missing),
            CalculatedAt = DateTime.Now
        };
    }

    private MatchScoreResponse BuildResponseInMemory(MatchScore score, HashSet<string> candidateSkills, JobDetail job)
    {
        var missing = JsonSerializer.Deserialize<List<string>>(score.MissingSkills) ?? new();

        return new MatchScoreResponse
        {
            JobId = job.JobId,
            JobTitle = job.Title,
            TotalScore = score.TotalScore,
            SkillScore = score.SkillScore,
            ExperienceScore = score.ExperienceScore,
            LocationScore = score.LocationScore,
            MatchedSkills = job.RequiredSkills.Where(s => candidateSkills.Contains(s.ToLowerInvariant())).ToList(),
            MissingSkills = missing
        };
    }

    private async Task<MatchScore?> CalculateWithDataAsync(Candidate candidate, HashSet<string> candidateSkills, int jobId)
    {
        var jobSkills = await _jobRepo.GetSkillNamesAsync(jobId);
        if (!jobSkills.Any()) return null;

        var jobSkillsNorm = jobSkills.Select(s => s.ToLowerInvariant()).ToList();
        var matched = jobSkillsNorm.Where(s => candidateSkills.Contains(s)).ToList();
        var missing = jobSkillsNorm.Except(matched).ToList();

        var skillScore = jobSkillsNorm.Count > 0
            ? Math.Round((decimal)matched.Count / jobSkillsNorm.Count * 100, 2)
            : 0m;

        var requiredExp = await _jobRepo.GetMinExperienceAsync(jobId);
        var expScore = requiredExp == 0 ? 100m
            : Math.Min(Math.Round((decimal)candidate.ExperienceYears / requiredExp * 100, 2), 100m);

        var jobLocation = await _jobRepo.GetLocationAsync(jobId);
        var locationScore = string.Equals(candidate.Location?.Trim(), jobLocation?.Trim(), StringComparison.OrdinalIgnoreCase) ? 100m : 0m;

        return new MatchScore
        {
            CandidateId = candidate.CandidateId,
            JobId = jobId,
            SkillScore = skillScore,
            ExperienceScore = expScore,
            LocationScore = locationScore,
            TotalScore = Math.Round((skillScore * 0.6m) + (expScore * 0.3m) + (locationScore * 0.1m), 2),
            MissingSkills = JsonSerializer.Serialize(missing),
            CalculatedAt = DateTime.Now
        };
    }

    private async Task<MatchScoreResponse> BuildResponseWithDataAsync(MatchScore score, HashSet<string> candidateSkills, int jobId)
    {
        var allJobSkills = await _jobRepo.GetSkillNamesAsync(jobId);
        var missing = JsonSerializer.Deserialize<List<string>>(score.MissingSkills) ?? new();
        var jobTitle = await _jobRepo.GetTitleAsync(jobId) ?? string.Empty;

        return new MatchScoreResponse
        {
            JobId = jobId,
            JobTitle = jobTitle,
            TotalScore = score.TotalScore,
            SkillScore = score.SkillScore,
            ExperienceScore = score.ExperienceScore,
            LocationScore = score.LocationScore,
            MatchedSkills = allJobSkills.Where(s => candidateSkills.Contains(s.ToLowerInvariant())).ToList(),
            MissingSkills = missing
        };
    }

    //  Core weighted algorithm 
    // Score = (SkillMatch × 0.6) + (ExperienceMatch × 0.3) + (LocationMatch × 0.1)
    private async Task<MatchScore?> CalculateAsync(Candidate candidate, int jobId)
    {
        var jobSkills = await _jobRepo.GetSkillNamesAsync(jobId);
        if (!jobSkills.Any()) return null;

        var candidateSkillNames = (await _candidateRepo.GetSkillsAsync(candidate.CandidateId))
            .Select(s => s.SkillName.ToLowerInvariant())
            .ToHashSet();

        var jobSkillsNorm = jobSkills
            .Select(s => s.ToLowerInvariant())
            .ToList();

        var matched = jobSkillsNorm
            .Where(s => candidateSkillNames.Contains(s))
            .ToList();

        var missing = jobSkillsNorm
            .Except(matched)
            .ToList();

        // Skill score (0–100)
        var skillScore = jobSkillsNorm.Count > 0
            ? Math.Round((decimal)matched.Count / jobSkillsNorm.Count * 100, 2)
            : 0m;

        // Experience score (0–100, capped)
        var requiredExp = await _jobRepo.GetMinExperienceAsync(jobId);
        var expScore = requiredExp == 0 ? 100m
            : Math.Min(Math.Round((decimal)candidate.ExperienceYears / requiredExp * 100, 2), 100m);

        // Location score (exact match = 100, else 0)
        var jobLocation = await _jobRepo.GetLocationAsync(jobId);
        var locationScore = string.Equals(
            candidate.Location?.Trim(),
            jobLocation?.Trim(),
            StringComparison.OrdinalIgnoreCase) ? 100m : 0m;

        // Weighted total
        var total = Math.Round(
            (skillScore * 0.6m) + (expScore * 0.3m) + (locationScore * 0.1m), 2);

        return new MatchScore
        {
            CandidateId = candidate.CandidateId,
            JobId = jobId,
            SkillScore = skillScore,
            ExperienceScore = expScore,
            LocationScore = locationScore,
            TotalScore = total,
            MissingSkills = JsonSerializer.Serialize(missing),
            CalculatedAt = DateTime.Now
        };
    }

    private async Task<MatchScoreResponse> BuildResponseAsync(
        MatchScore score, int candidateId, int jobId)
    {
        var allJobSkills = await _jobRepo.GetSkillNamesAsync(jobId);
        var candidateSkills = (await _candidateRepo.GetSkillsAsync(candidateId))
            .Select(s => s.SkillName.ToLowerInvariant())
            .ToHashSet();

        var missing = JsonSerializer.Deserialize<List<string>>(score.MissingSkills) ?? new();
        var jobTitle = await _jobRepo.GetTitleAsync(jobId) ?? string.Empty;

        return new MatchScoreResponse
        {
            JobId = jobId,
            JobTitle = jobTitle,
            TotalScore = score.TotalScore,
            SkillScore = score.SkillScore,
            ExperienceScore = score.ExperienceScore,
            LocationScore = score.LocationScore,
            MatchedSkills = allJobSkills
                .Where(s => candidateSkills.Contains(s.ToLowerInvariant()))
                .ToList(),
            MissingSkills = missing
        };
    }
}