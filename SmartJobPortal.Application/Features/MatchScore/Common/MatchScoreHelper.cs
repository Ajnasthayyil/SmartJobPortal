using SmartJobPortal.Application.Common.Utilities;
using SmartJobPortal.Application.DTOs.Candidate;
using SmartJobPortal.Application.Interfaces;
using SmartJobPortal.Domain.Entities;
using System.Text.Json;

namespace SmartJobPortal.Application.Features.MatchScore.Common;

public class MatchScoreHelper
{
    private readonly ICandidateRepository _candidateRepo;
    private readonly IJobRepository _jobRepo;
    private readonly ISemanticMatcher _matcher;

    public MatchScoreHelper(ICandidateRepository candidateRepo, IJobRepository jobRepo, ISemanticMatcher matcher)
    {
        _candidateRepo = candidateRepo;
        _jobRepo = jobRepo;
        _matcher = matcher;
    }

    public int CalculateRoleRelevance(string jobTitle, List<string> candidateRoles)
    {
        if (string.IsNullOrWhiteSpace(jobTitle) || candidateRoles == null || !candidateRoles.Any())
            return 0;

        var jobTitleNorm = jobTitle.ToLowerInvariant().Trim();
        var candidateRolesNorm = candidateRoles.Select(r => r.ToLowerInvariant().Trim()).ToList();

        var families = new Dictionary<string, List<string>>
        {
            ["Frontend Developer"] = new() { "frontend developer", "ui developer", "angular developer", "react developer", "web developer", "frontend engineer" },
            ["Backend Developer"] = new() { "backend developer", ".net developer", "asp.net developer", "api developer", "backend engineer", "c# developer" },
            ["Full Stack Developer"] = new() { "full stack developer", "software engineer", "application developer", "software developer" },
            ["Data Analyst"] = new() { "data analyst", "business analyst", "data scientist", "bi analyst" },
            ["Accountant"] = new() { "accountant", "financial accountant", "auditor", "tax accountant" }
        };

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
            if (jobFamilyKey != null && (families[jobFamilyKey].Any(f => role.Contains(f)) || role.Contains(jobFamilyKey.ToLowerInvariant())))
            {
                currentRelevance = 90;
                if (role == jobTitleNorm) currentRelevance = 100;
            }
            else
            {
                var jobWords = jobTitleNorm.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                var roleWords = role.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                var commonWords = jobWords.Intersect(roleWords).Count();
                if (commonWords > 0)
                {
                    currentRelevance = Math.Min(40 + (commonWords * 15), 75);
                }
            }
            if (currentRelevance > highestRelevance)
                highestRelevance = currentRelevance;
        }
        return highestRelevance;
    }

    public async Task<SmartJobPortal.Domain.Entities.MatchScore?> CalculateAsync(SmartJobPortal.Domain.Entities.Candidate candidate, int jobId)
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
        var skillScore = jobSkills.Count > 0 ? Math.Round((decimal)matched.Count / jobSkills.Count * 100, 2) : 0m;

        var jobTitle = await _jobRepo.GetTitleAsync(jobId) ?? "";
        var experiences = await _candidateRepo.GetExperienceAsync(candidate.CandidateId);
        var candidateRoles = experiences.Select(e => e.Role).ToList();

        var roleRelevance = CalculateRoleRelevance(jobTitle, candidateRoles);
        var requiredExp = await _jobRepo.GetMinExperienceAsync(jobId);
        var expSufficiency = requiredExp == 0 ? 100m : Math.Min(Math.Round((decimal)candidate.ExperienceYears / requiredExp * 100, 2), 100m);

        var relevantExpScore = Math.Round((roleRelevance * expSufficiency) / 100m, 2);

        var jobLocation = await _jobRepo.GetLocationAsync(jobId);
        var locationScore = string.Equals(candidate.Location?.Trim(), jobLocation?.Trim(), StringComparison.OrdinalIgnoreCase) ? 100m : 0m;

        var total = Math.Round((skillScore * 0.6m) + (relevantExpScore * 0.3m) + (locationScore * 0.1m), 2);

        return new SmartJobPortal.Domain.Entities.MatchScore
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

    public async Task<MatchScoreResponse> BuildResponseAsync(SmartJobPortal.Domain.Entities.MatchScore score, int candidateId, int jobId)
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
