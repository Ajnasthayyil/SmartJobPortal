using SmartJobPortal.Application.Common.Utilities;
using SmartJobPortal.Application.DTOs.Candidate;
using SmartJobPortal.Application.Interfaces;
using System.Text.Json;

namespace SmartJobPortal.Application.Features.MatchScore.Common;

/// <summary>
/// Calculates a weighted match score (0–100) between a candidate and a job.
///
/// Weights:
///   Skills         60% — Semantic skill overlap via SemanticMatcher
///   Experience     30% — Role relevance × years-of-experience sufficiency
///   Location       10% — Fuzzy location match (exact / contains / remote-friendly)
///
/// Bug Fixes Applied:
///   [1] Jobs with no skills no longer return null — a partial score is computed.
///   [2] Role relevance covers 25+ job families instead of 5.
///   [3] Location scoring is fuzzy (100 / 80 / 60 / 0) instead of binary.
/// </summary>
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

    // ── Role Relevance ────────────────────────────────────────────────────────
    // Maps canonical role families to all known titles that belong to each family.
    // Lookups are bidirectional: if the job title is in a family, all candidate
    // roles in the same family receive high relevance.
    private static readonly Dictionary<string, HashSet<string>> _roleFamilies =
        new(StringComparer.OrdinalIgnoreCase)
    {
        ["Frontend Developer"] = new(OIC)
        {
            "frontend developer", "ui developer", "angular developer", "react developer",
            "vue developer", "web developer", "frontend engineer", "ui engineer",
            "web ui developer", "front end developer", "front-end developer",
            "html developer", "javascript developer", "next.js developer"
        },
        ["Backend Developer"] = new(OIC)
        {
            "backend developer", ".net developer", "asp.net developer", "api developer",
            "backend engineer", "c# developer", "java developer", "python developer",
            "node.js developer", "node developer", "php developer", "go developer",
            "golang developer", "ruby developer", "spring developer", "laravel developer",
            "server-side developer", "back-end developer", "back end developer"
        },
        ["Full Stack Developer"] = new(OIC)
        {
            "full stack developer", "software engineer", "application developer",
            "software developer", "full-stack developer", "full stack engineer",
            "web application developer", "mern developer", "mean developer",
            "full stack web developer", "software development engineer"
        },
        ["Mobile Developer"] = new(OIC)
        {
            "mobile developer", "android developer", "ios developer", "swift developer",
            "kotlin developer", "react native developer", "flutter developer",
            "mobile app developer", "mobile application developer", "cross-platform developer"
        },
        ["Data Analyst"] = new(OIC)
        {
            "data analyst", "business analyst", "data scientist", "bi analyst",
            "business intelligence analyst", "reporting analyst", "insights analyst",
            "analytics engineer", "data engineer"
        },
        ["Data Scientist"] = new(OIC)
        {
            "data scientist", "ml engineer", "machine learning engineer",
            "ai engineer", "ai/ml engineer", "deep learning engineer",
            "research scientist", "applied scientist", "data science engineer"
        },
        ["DevOps Engineer"] = new(OIC)
        {
            "devops engineer", "site reliability engineer", "sre", "cloud engineer",
            "infrastructure engineer", "platform engineer", "build and release engineer",
            "systems reliability engineer", "devsecops engineer", "cloud devops engineer"
        },
        ["Cloud Architect"] = new(OIC)
        {
            "cloud architect", "solutions architect", "aws architect", "azure architect",
            "gcp architect", "cloud infrastructure architect", "enterprise architect"
        },
        ["QA Engineer"] = new(OIC)
        {
            "qa engineer", "quality assurance engineer", "test engineer", "software tester",
            "automation test engineer", "sdet", "software development engineer in test",
            "qa analyst", "quality analyst", "performance test engineer"
        },
        ["Database Administrator"] = new(OIC)
        {
            "database administrator", "dba", "sql developer", "database developer",
            "database engineer", "data warehouse engineer", "sql server dba", "oracle dba"
        },
        ["Cybersecurity Engineer"] = new(OIC)
        {
            "cybersecurity engineer", "security engineer", "information security engineer",
            "penetration tester", "pen tester", "ethical hacker", "security analyst",
            "soc analyst", "network security engineer", "cloud security engineer"
        },
        ["Project Manager"] = new(OIC)
        {
            "project manager", "program manager", "delivery manager", "it project manager",
            "technical project manager", "scrum master", "agile coach", "product owner",
            "release manager", "pmo", "project lead"
        },
        ["Product Manager"] = new(OIC)
        {
            "product manager", "product owner", "senior product manager",
            "associate product manager", "technical product manager", "growth product manager"
        },
        ["UI/UX Designer"] = new(OIC)
        {
            "ui/ux designer", "ui designer", "ux designer", "product designer",
            "interaction designer", "visual designer", "graphic designer",
            "web designer", "ux researcher", "design lead"
        },
        ["Network Engineer"] = new(OIC)
        {
            "network engineer", "network administrator", "cisco engineer",
            "network architect", "network security engineer", "system administrator",
            "sysadmin", "it infrastructure engineer"
        },
        ["Embedded Systems Engineer"] = new(OIC)
        {
            "embedded systems engineer", "firmware engineer", "iot developer",
            "c++ developer", "embedded software engineer", "rtos developer",
            "embedded c developer"
        },
        ["Machine Learning Engineer"] = new(OIC)
        {
            "machine learning engineer", "ml engineer", "ai engineer",
            "deep learning engineer", "nlp engineer", "computer vision engineer",
            "research engineer"
        },
        ["Technical Writer"] = new(OIC)
        {
            "technical writer", "documentation specialist", "technical documentation engineer",
            "api writer", "content developer"
        },
        ["Systems Analyst"] = new(OIC)
        {
            "systems analyst", "it analyst", "business systems analyst",
            "enterprise systems analyst", "solution analyst"
        },
        ["Accountant"] = new(OIC)
        {
            "accountant", "financial accountant", "auditor", "tax accountant",
            "cost accountant", "chartered accountant", "ca", "cpa", "finance executive"
        },
        ["Sales Engineer"] = new(OIC)
        {
            "sales engineer", "solutions engineer", "presales engineer",
            "technical sales engineer", "customer success engineer"
        },
        ["Support Engineer"] = new(OIC)
        {
            "support engineer", "technical support engineer", "helpdesk engineer",
            "customer support engineer", "application support engineer", "l1 support", "l2 support"
        },
        ["Blockchain Developer"] = new(OIC)
        {
            "blockchain developer", "smart contract developer", "solidity developer",
            "web3 developer", "cryptocurrency developer", "ethereum developer"
        },
        ["Game Developer"] = new(OIC)
        {
            "game developer", "unity developer", "unreal engine developer",
            "game engineer", "game programmer", "3d developer"
        }
    };

    private static StringComparer OIC => StringComparer.OrdinalIgnoreCase;

    // ── Public API ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Computes role relevance (0–100) between a job title and the candidate's
    /// historical roles, using the expanded family dictionary + fallback word scoring.
    /// </summary>
    public int CalculateRoleRelevance(string jobTitle, List<string> candidateRoles)
    {
        if (string.IsNullOrWhiteSpace(jobTitle) || candidateRoles == null || !candidateRoles.Any())
            return 0;

        var jobTitleNorm = jobTitle.ToLowerInvariant().Trim();

        // Find which family the job title belongs to
        string? jobFamilyKey = null;
        foreach (var family in _roleFamilies)
        {
            bool inFamily = family.Value.Any(alias => jobTitleNorm.Contains(alias) || alias.Contains(jobTitleNorm))
                         || jobTitleNorm.Contains(family.Key.ToLowerInvariant());
            if (inFamily) { jobFamilyKey = family.Key; break; }
        }

        int highest = 0;
        foreach (var role in candidateRoles)
        {
            if (string.IsNullOrWhiteSpace(role)) continue;
            var roleNorm = role.ToLowerInvariant().Trim();
            int score = 0;

            // Exact title match
            if (roleNorm == jobTitleNorm)
            {
                score = 100;
            }
            // Same job family
            else if (jobFamilyKey != null && _roleFamilies[jobFamilyKey]
                        .Any(alias => roleNorm.Contains(alias) || alias.Contains(roleNorm)))
            {
                score = 90;
            }
            // Cross-family check: candidate role appears in ANY family that overlaps with job family
            else if (jobFamilyKey == null)
            {
                // Job title unknown family — try substring match as primary signal
                if (roleNorm.Contains(jobTitleNorm) || jobTitleNorm.Contains(roleNorm))
                    score = 80;
            }
            else
            {
                // Token word intersection fallback
                var jobTokens  = jobTitleNorm.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                var roleTokens = roleNorm.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                var common     = jobTokens.Intersect(roleTokens, OIC).Count();

                // Give partial credit for sharing meaningful tokens like "engineer", "developer"
                var meaningful = new[] { "developer", "engineer", "architect", "analyst", "manager", "designer", "specialist" };
                bool sharesMeaningful = jobTokens.Intersect(roleTokens, OIC).Any(t => meaningful.Contains(t, OIC));

                if (common >= 2)
                    score = 75;
                else if (common == 1 && sharesMeaningful)
                    score = 55;
            }

            if (score > highest) highest = score;
        }

        return highest;
    }

    /// <summary>
    /// Full calculation pipeline: skills + experience + location → TotalScore.
    /// FIX [1]: Jobs with zero skills return a partial score instead of null.
    /// </summary>
    public async Task<SmartJobPortal.Domain.Entities.MatchScore> CalculateAsync(
        SmartJobPortal.Domain.Entities.Candidate candidate, int jobId)
    {
        var jobSkills = await _jobRepo.GetSkillNamesAsync(jobId);

        // FIX [1]: Don't bail out when job has no skills — compute partial score
        decimal skillScore = 0m;
        var matched = new List<string>();
        var missing = new List<string>();

        if (jobSkills.Any())
        {
            var candidateSkills = (await _candidateRepo.GetSkillsAsync(candidate.CandidateId))
                .Select(s => s.SkillName)
                .ToList();

            foreach (var js in jobSkills)
            {
                if (candidateSkills.Any(cs => _matcher.IsMatch(cs, js, out _)))
                    matched.Add(js.ToLowerInvariant());
            }
            missing  = jobSkills.Select(s => s.ToLowerInvariant()).Except(matched).ToList();
            skillScore = Math.Round((decimal)matched.Count / jobSkills.Count * 100, 2);
        }

        var jobTitle    = await _jobRepo.GetTitleAsync(jobId) ?? string.Empty;
        var experiences = await _candidateRepo.GetExperienceAsync(candidate.CandidateId);
        var candidateRoles = experiences.Select(e => e.Role).ToList();

        var roleRelevance   = CalculateRoleRelevance(jobTitle, candidateRoles);
        var requiredExp     = await _jobRepo.GetMinExperienceAsync(jobId);
        var expSufficiency  = requiredExp == 0
            ? 100m
            : Math.Min(Math.Round((decimal)candidate.ExperienceYears / requiredExp * 100, 2), 100m);

        var relevantExpScore = Math.Round((roleRelevance * expSufficiency) / 100m, 2);

        var jobLocation  = await _jobRepo.GetLocationAsync(jobId);
        var locationScore = CalculateLocationScore(candidate.Location, jobLocation);

        var total = Math.Round((skillScore * 0.6m) + (relevantExpScore * 0.3m) + (locationScore * 0.1m), 2);

        return new SmartJobPortal.Domain.Entities.MatchScore
        {
            CandidateId   = candidate.CandidateId,
            JobId         = jobId,
            SkillScore    = skillScore,
            ExperienceScore = relevantExpScore,
            LocationScore = locationScore,
            TotalScore    = total,
            MissingSkills = JsonSerializer.Serialize(missing),
            CalculatedAt  = DateTime.Now
        };
    }

    public async Task<MatchScoreResponse> BuildResponseAsync(
        SmartJobPortal.Domain.Entities.MatchScore score, int candidateId, int jobId)
    {
        var allJobSkills     = await _jobRepo.GetSkillNamesAsync(jobId);
        var candidateSkills  = (await _candidateRepo.GetSkillsAsync(candidateId))
            .Select(s => s.SkillName.ToLowerInvariant())
            .ToHashSet();

        var missing  = JsonSerializer.Deserialize<List<string>>(score.MissingSkills) ?? new();
        var jobTitle = await _jobRepo.GetTitleAsync(jobId) ?? string.Empty;

        return new MatchScoreResponse
        {
            JobId         = jobId,
            JobTitle      = jobTitle,
            TotalScore    = score.TotalScore,
            SkillScore    = score.SkillScore,
            ExperienceScore = score.ExperienceScore,
            LocationScore = score.LocationScore,
            MatchedSkills = allJobSkills
                .Where(js => candidateSkills.Any(cs => _matcher.IsMatch(cs, js, out _)))
                .ToList(),
            MissingSkills = missing
        };
    }

    // ── Location Scoring (FIX [3] — fuzzy instead of binary) ──────────────────

    /// <summary>
    /// Returns a graded location score:
    ///   100 — exact match (case-insensitive)
    ///    80 — one location contains the other (e.g. "Bengaluru" vs "Bengaluru, India")
    ///    60 — either party is "remote" / "work from home"
    ///     0 — no relationship found
    /// </summary>
    public static decimal CalculateLocationScore(string? candidateLocation, string? jobLocation)
    {
        if (string.IsNullOrWhiteSpace(candidateLocation) || string.IsNullOrWhiteSpace(jobLocation))
            return 0m;

        var c = candidateLocation.Trim().ToLowerInvariant();
        var j = jobLocation.Trim().ToLowerInvariant();

        // Exact
        if (c == j) return 100m;

        // Common city-name aliases
        c = NormalizeCityAlias(c);
        j = NormalizeCityAlias(j);
        if (c == j) return 100m;

        // One contains the other (e.g. "Bangalore" in "Bangalore, Karnataka")
        if (c.Contains(j) || j.Contains(c)) return 80m;

        // Remote-friendly
        var remoteKeywords = new[] { "remote", "work from home", "wfh", "anywhere", "distributed" };
        if (remoteKeywords.Any(k => c.Contains(k) || j.Contains(k))) return 60m;

        return 0m;
    }

    private static readonly Dictionary<string, string> _cityAliases = new(StringComparer.OrdinalIgnoreCase)
    {
        ["bengaluru"]  = "bangalore",
        ["bengalore"]  = "bangalore",
        ["mumbai"]     = "bombay",
        ["kolkata"]    = "calcutta",
        ["chennai"]    = "madras",
        ["new york city"] = "new york",
        ["nyc"]        = "new york",
        ["la"]         = "los angeles",
        ["sf"]         = "san francisco",
        ["dc"]         = "washington",
        ["bangalore"]  = "bangalore",   // self-mapping for normalisation pass
    };

    private static string NormalizeCityAlias(string city)
    {
        foreach (var alias in _cityAliases)
            city = city.Replace(alias.Key, alias.Value, StringComparison.OrdinalIgnoreCase);
        return city.Trim();
    }
}
