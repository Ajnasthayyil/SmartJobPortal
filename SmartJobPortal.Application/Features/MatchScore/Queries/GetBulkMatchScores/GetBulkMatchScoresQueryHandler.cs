using MediatR;
using SmartJobPortal.Application.Common;
using SmartJobPortal.Application.DTOs.Candidate;
using SmartJobPortal.Application.Features.MatchScore.Common;
using SmartJobPortal.Application.Interfaces;
using SmartJobPortal.Domain.Entities;
using System.Text.Json;

namespace SmartJobPortal.Application.Features.MatchScore.Queries.GetBulkMatchScores;

public class GetBulkMatchScoresQueryHandler : IRequestHandler<GetBulkMatchScoresQuery, ApiResponse<List<MatchScoreResponse>>>
{
    private readonly ICandidateRepository _candidateRepo;
    private readonly IJobRepository _jobRepo;
    private readonly IMatchScoreRepository _matchScoreRepo;
    private readonly ICacheService _cache;
    private readonly MatchScoreHelper _helper;
    private readonly SmartJobPortal.Application.Common.Utilities.ISemanticMatcher _matcher;

    public GetBulkMatchScoresQueryHandler(
        ICandidateRepository candidateRepo,
        IJobRepository jobRepo,
        IMatchScoreRepository matchScoreRepo,
        ICacheService cache,
        SmartJobPortal.Application.Common.Utilities.ISemanticMatcher matcher)
    {
        _candidateRepo = candidateRepo;
        _jobRepo = jobRepo;
        _matchScoreRepo = matchScoreRepo;
        _cache = cache;
        _matcher = matcher;
        _helper = new MatchScoreHelper(candidateRepo, jobRepo, matcher);
    }

    public async Task<ApiResponse<List<MatchScoreResponse>>> Handle(GetBulkMatchScoresQuery request, CancellationToken cancellationToken)
    {
        var jobIds = request.JobIds;
        if (jobIds == null || !jobIds.Any())
            return ApiResponse<List<MatchScoreResponse>>.Ok(new());

        var candidate = await _candidateRepo.GetByUserIdAsync(request.UserId);
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

        var experiences = await _candidateRepo.GetExperienceAsync(candidate.CandidateId);
        var candidateRoles = experiences.Select(e => e.Role).ToList();

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

    private SmartJobPortal.Domain.Entities.MatchScore CalculateInMemory(SmartJobPortal.Domain.Entities.Candidate candidate, HashSet<string> candidateSkills, List<string> candidateRoles, JobDetail job)
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

        var roleRelevance = _helper.CalculateRoleRelevance(job.Title, candidateRoles);
        var expSufficiency = job.MinExperienceYears == 0 ? 100m
            : Math.Min(Math.Round((decimal)candidate.ExperienceYears / job.MinExperienceYears * 100, 2), 100m);
        
        var relevantExpScore = Math.Round((roleRelevance * expSufficiency) / 100m, 2);

        var locationScore = string.Equals(candidate.Location?.Trim(), job.Location?.Trim(), StringComparison.OrdinalIgnoreCase) ? 100m : 0m;

        return new SmartJobPortal.Domain.Entities.MatchScore
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

    private MatchScoreResponse BuildResponseInMemory(SmartJobPortal.Domain.Entities.MatchScore score, HashSet<string> candidateSkills, JobDetail job)
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
}
