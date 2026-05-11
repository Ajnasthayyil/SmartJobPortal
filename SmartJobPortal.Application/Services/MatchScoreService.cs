using System.Text.Json;
using SmartJobPortal.Application.Common;
using SmartJobPortal.Application.Common.Utilities;
using SmartJobPortal.Application.DTOs.Candidate;
using SmartJobPortal.Application.Interfaces;
using SmartJobPortal.Domain.Entities;

namespace SmartJobPortal.Application.Services;

public class MatchScoreService : IMatchScoreService
{
    private readonly ICandidateRepository _candidateRepo;
    private readonly IJobRepository _jobRepo;
    private readonly IMatchScoreRepository _matchScoreRepo;
    private readonly ISemanticMatcher _matcher;
    private readonly ICacheService _cache;

    public MatchScoreService(
        ICandidateRepository candidateRepo,
        IJobRepository jobRepo,
        IMatchScoreRepository matchScoreRepo,
        ISemanticMatcher matcher,
        ICacheService cache)
    {
        _candidateRepo = candidateRepo;
        _jobRepo = jobRepo;
        _matchScoreRepo = matchScoreRepo;
        _matcher = matcher;
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
        {
            return ApiResponse<List<MatchScoreResponse>>.Ok(jobIds.Select(id => new MatchScoreResponse
            {
                JobId = id,
                TotalScore = 0,
                SkillScore = 0,
                ExperienceScore = 0,
                LocationScore = 0,
                MatchedSkills = new(),
                MissingSkills = new()
            }).ToList());
        }

        var results = new List<MatchScoreResponse>();
        var candidateSkills = (await _candidateRepo.GetSkillsAsync(candidate.CandidateId))
            .Select(s => s.SkillName.ToLowerInvariant())
            .ToHashSet();

        // Fetch candidate roles once for bulk calculation
        var experiences = await _candidateRepo.GetExperienceAsync(candidate.CandidateId);
        var candidateRoles = experiences.Select(e => e.Role).ToList();

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

            var score = CalculateInMemory(candidate, candidateSkills, candidateRoles, job);
            await _matchScoreRepo.UpsertAsync(score);
            
            var response = BuildResponseInMemory(score, candidateSkills, job);
            await _cache.SetAsync($"match:{candidate.CandidateId}:{jobId}", response, TimeSpan.FromHours(1));
            results.Add(response);
        }

        return ApiResponse<List<MatchScoreResponse>>.Ok(results);
    }

    /// <summary>
    /// Calculates role relevance based on keyword families and partial overlaps.
    /// Returns a score between 0 and 100.
    /// </summary>
    private int CalculateRoleRelevance(string jobTitle, List<string> candidateRoles)
    {
        if (string.IsNullOrWhiteSpace(jobTitle) || candidateRoles == null || !candidateRoles.Any())
            return 0;

        var jobTitleNorm = jobTitle.ToLowerInvariant().Trim();
        var candidateRolesNorm = candidateRoles.Select(r => r.ToLowerInvariant().Trim()).ToList();

        // Define Role Families for cross-referencing
        var families = new Dictionary<string, List<string>>
        {
            ["Frontend Developer"] = new() { "frontend developer", "ui developer", "angular developer", "react developer", "web developer", "frontend engineer" },
            ["Backend Developer"] = new() { "backend developer", ".net developer", "asp.net developer", "api developer", "backend engineer", "c# developer" },
            ["Full Stack Developer"] = new() { "full stack developer", "software engineer", "application developer", "software developer" },
            ["Data Analyst"] = new() { "data analyst", "business analyst", "data scientist", "bi analyst" },
            ["Accountant"] = new() { "accountant", "financial accountant", "auditor", "tax accountant" }
        };

        // Determine the family of the job being viewed
        string? jobFamilyKey = null;
        foreach (var family in families)
        {
            if (family.Value.Any(f => jobTitleNorm.Contains(f)) || jobTitleNorm.Contains(family.Key.ToLowerInvariant()))
            {
                jobFamilyKey = family.Key;
                break;
            }
        }

        int highestRelevance = 0;

        foreach (var role in candidateRolesNorm)
        {
            int currentRelevance = 0;

            // 1. Precise Family Match (90-100)
            if (jobFamilyKey != null && (families[jobFamilyKey].Any(f => role.Contains(f)) || role.Contains(jobFamilyKey.ToLowerInvariant())))
            {
                currentRelevance = 90;
                if (role == jobTitleNorm) currentRelevance = 100;
            }
            // 2. Partial Keyword Overlap (40-70)
            else
            {
                var jobWords = jobTitleNorm.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                var roleWords = role.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                var commonWords = jobWords.Intersect(roleWords).Count();

                if (commonWords > 0)
                {
                    // Basic keyword overlap logic
                    currentRelevance = Math.Min(40 + (commonWords * 15), 75);
                }
            }

            if (currentRelevance > highestRelevance)
                highestRelevance = currentRelevance;
        }

        return highestRelevance;
    }

    private MatchScore CalculateInMemory(Candidate candidate, HashSet<string> candidateSkills, List<string> candidateRoles, JobDetail job)
    {
        var jobSkills = job.RequiredSkills;
        var candidateSkillsList = candidateSkills.ToList();
        
        var matched = new List<string>();
        foreach (var js in jobSkills)
        {
            if (candidateSkillsList.Any(cs => _matcher.IsMatch(cs, js, out _)))
            {
                matched.Add(js.ToLowerInvariant());
            }
        }
        
        var missing = jobSkills.Select(s => s.ToLowerInvariant()).Except(matched).ToList();

        var skillScore = jobSkills.Count > 0
            ? Math.Round((decimal)matched.Count / jobSkills.Count * 100, 2)
            : 0m;

        // Relevant Experience Logic
        var roleRelevance = CalculateRoleRelevance(job.Title, candidateRoles);
        var expSufficiency = job.MinExperienceYears == 0 ? 100m
            : Math.Min(Math.Round((decimal)candidate.ExperienceYears / job.MinExperienceYears * 100, 2), 100m);
        
        var relevantExpScore = Math.Round((roleRelevance * expSufficiency) / 100m, 2);

        var locationScore = string.Equals(candidate.Location?.Trim(), job.Location?.Trim(), StringComparison.OrdinalIgnoreCase) ? 100m : 0m;

        return new MatchScore
        {
            CandidateId = candidate.CandidateId,
            JobId = job.JobId,
            SkillScore = skillScore,
            ExperienceScore = relevantExpScore,
            LocationScore = locationScore,
            TotalScore = Math.Round((skillScore * 0.6m) + (relevantExpScore * 0.3m) + (locationScore * 0.1m), 2),
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
            MatchedSkills = job.RequiredSkills.Where(js => candidateSkills.Any(cs => _matcher.IsMatch(cs, js, out _))).ToList(),
            MissingSkills = missing
        };
    }

    private async Task<MatchScore?> CalculateAsync(Candidate candidate, int jobId)
    {
        var jobSkills = await _jobRepo.GetSkillNamesAsync(jobId);
        if (!jobSkills.Any()) return null;

        var candidateSkills = (await _candidateRepo.GetSkillsAsync(candidate.CandidateId))
            .Select(s => s.SkillName)
            .ToList();

        var matched = new List<string>();
        foreach (var js in jobSkills)
        {
            if (candidateSkills.Any(cs => _matcher.IsMatch(cs, js, out _)))
            {
                matched.Add(js.ToLowerInvariant());
            }
        }

        var missing = jobSkills.Select(s => s.ToLowerInvariant()).Except(matched).ToList();

        // 1. Skill score (60%)
        var skillScore = jobSkills.Count > 0
            ? Math.Round((decimal)matched.Count / jobSkills.Count * 100, 2)
            : 0m;

        // 2. Relevant Experience score (30%)
        var jobTitle = await _jobRepo.GetTitleAsync(jobId) ?? "";
        var experiences = await _candidateRepo.GetExperienceAsync(candidate.CandidateId);
        var candidateRoles = experiences.Select(e => e.Role).ToList();

        var roleRelevance = CalculateRoleRelevance(jobTitle, candidateRoles);
        var requiredExp = await _jobRepo.GetMinExperienceAsync(jobId);
        var expSufficiency = requiredExp == 0 ? 100m
            : Math.Min(Math.Round((decimal)candidate.ExperienceYears / requiredExp * 100, 2), 100m);

        var relevantExpScore = Math.Round((roleRelevance * expSufficiency) / 100m, 2);

        // 3. Location score (10%)
        var jobLocation = await _jobRepo.GetLocationAsync(jobId);
        var locationScore = string.Equals(
            candidate.Location?.Trim(),
            jobLocation?.Trim(),
            StringComparison.OrdinalIgnoreCase) ? 100m : 0m;

        // Weighted total
        var total = Math.Round(
            (skillScore * 0.6m) + (relevantExpScore * 0.3m) + (locationScore * 0.1m), 2);

        return new MatchScore
        {
            CandidateId = candidate.CandidateId,
            JobId = jobId,
            SkillScore = skillScore,
            ExperienceScore = relevantExpScore,
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
                .Where(js => candidateSkills.Any(cs => _matcher.IsMatch(cs, js, out _)))
                .ToList(),
            MissingSkills = missing
        };
    }
}