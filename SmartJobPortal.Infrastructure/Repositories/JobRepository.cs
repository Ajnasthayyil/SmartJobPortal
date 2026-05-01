using Dapper;
using SmartJobPortal.Application.DTOs.Candidate;
using SmartJobPortal.Application.Interfaces;
using SmartJobPortal.Infrastructure.Data;

namespace SmartJobPortal.Infrastructure.Repositories;

public class JobRepository : IJobRepository
{
    private readonly IDbConnectionFactory _factory;

    public JobRepository(IDbConnectionFactory factory) => _factory = factory;

    public async Task<(List<JobListItem> jobs, int totalCount)> SearchAsync(
        JobSearchRequest req)
    {
        using var conn = _factory.CreateConnection();

        var where = new List<string> { "j.IsActive = 1" };
        var p = new DynamicParameters();

        if (!string.IsNullOrWhiteSpace(req.Keyword))
        {
            where.Add("(j.Title LIKE @Keyword OR j.Description LIKE @Keyword)");
            p.Add("Keyword", $"%{req.Keyword}%");
        }
        if (!string.IsNullOrWhiteSpace(req.Location))
        {
            where.Add("j.Location LIKE @Location");
            p.Add("Location", $"%{req.Location}%");
        }
        if (!string.IsNullOrWhiteSpace(req.JobType))
        {
            where.Add("j.JobType = @JobType");
            p.Add("JobType", req.JobType);
        }
        if (req.MinSalary.HasValue)
        {
            where.Add("j.MaxSalary >= @MinSalary");
            p.Add("MinSalary", req.MinSalary);
        }
        if (req.MaxSalary.HasValue)
        {
            where.Add("j.MinSalary <= @MaxSalary");
            p.Add("MaxSalary", req.MaxSalary);
        }
        if (req.MinExperience.HasValue)
        {
            where.Add("j.MinExperienceYears >= @MinExp");
            p.Add("MinExp", req.MinExperience);
        }
        if (req.MaxExperience.HasValue)
        {
            where.Add("j.MinExperienceYears <= @MaxExp");
            p.Add("MaxExp", req.MaxExperience);
        }

        var whereClause = string.Join(" AND ", where);
        var offset = (req.Page - 1) * req.PageSize;
        p.Add("Offset", offset);
        p.Add("PageSize", req.PageSize);

        var countSql = $"""
            SELECT COUNT(*)
            FROM Jobs j
            INNER JOIN Recruiters r ON r.RecruiterId = j.RecruiterId
            WHERE {whereClause}
            """;

        var dataSql = $"""
            SELECT
                j.JobId, j.Title, r.CompanyName, j.Location,
                j.JobType, j.MinSalary, j.MaxSalary,
                j.MinExperienceYears, j.PostedAt
            FROM Jobs j
            INNER JOIN Recruiters r ON r.RecruiterId = j.RecruiterId
            WHERE {whereClause}
            ORDER BY j.PostedAt DESC
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY
            """;

        var total = await conn.ExecuteScalarAsync<int>(countSql, p);
        var jobs = (await conn.QueryAsync<JobListItem>(dataSql, p)).ToList();

        if (jobs.Any())
        {
            var jobIds = jobs.Select(j => j.JobId).ToList();
            var allSkills = await conn.QueryAsync<(int JobId, string Name)>(@"
                SELECT js.JobId, s.Name
                FROM JobSkills js
                INNER JOIN Skills s ON s.SkillId = js.SkillId
                WHERE js.JobId IN @JobIds", new { JobIds = jobIds });

            var skillMap = allSkills
                .GroupBy(x => x.JobId)
                .ToDictionary(g => g.Key, g => g.Select(x => x.Name).ToList());

            foreach (var job in jobs)
            {
                job.RequiredSkills = skillMap.ContainsKey(job.JobId) ? skillMap[job.JobId] : new List<string>();
            }
        }

        return (jobs, total);
    }

    public async Task<JobDetail?> GetDetailAsync(int jobId)
    {
        using var conn = _factory.CreateConnection();
        var job = await conn.QuerySingleOrDefaultAsync<JobDetail>("""
            SELECT
                j.JobId, j.Title, j.Description, r.CompanyName,
                j.Location, j.JobType, j.MinSalary, j.MaxSalary,
                j.MinExperienceYears, j.PostedAt, j.ExpiresAt
            FROM Jobs j
            INNER JOIN Recruiters r ON r.RecruiterId = j.RecruiterId
            WHERE j.JobId = @JobId AND j.IsActive = 1
            """, new { JobId = jobId });

        if (job != null)
            job.RequiredSkills = await GetSkillNamesAsync(jobId);

        return job;
    }

    public async Task<List<string>> GetSkillNamesAsync(int jobId)
    {
        using var conn = _factory.CreateConnection();
        var names = await conn.QueryAsync<string>("""
            SELECT s.Name
            FROM JobSkills js
            INNER JOIN Skills s ON s.SkillId = js.SkillId
            WHERE js.JobId = @JobId
            """, new { JobId = jobId });
        return names.ToList();
    }

    public async Task<int> GetMinExperienceAsync(int jobId)
    {
        using var conn = _factory.CreateConnection();
        return await conn.ExecuteScalarAsync<int>(
            "SELECT MinExperienceYears FROM Jobs WHERE JobId = @JobId",
            new { JobId = jobId });
    }

    public async Task<string?> GetLocationAsync(int jobId)
    {
        using var conn = _factory.CreateConnection();
        return await conn.ExecuteScalarAsync<string?>(
            "SELECT Location FROM Jobs WHERE JobId = @JobId",
            new { JobId = jobId });
    }

    public async Task<string?> GetTitleAsync(int jobId)
    {
        using var conn = _factory.CreateConnection();
        return await conn.ExecuteScalarAsync<string?>(
            "SELECT Title FROM Jobs WHERE JobId = @JobId",
            new { JobId = jobId });
    }

    public async Task<List<JobListItem>> GetBySkillsAsync(List<string> skills)
    {
        if (skills == null || !skills.Any()) return new List<JobListItem>();

        using var conn = _factory.CreateConnection();
        // Simple match: find jobs that require any of these skills
        var sql = @"
            SELECT DISTINCT j.*, r.CompanyName 
            FROM Jobs j
            INNER JOIN Recruiters r ON j.RecruiterId = r.RecruiterId
            INNER JOIN JobSkills js ON j.JobId = js.JobId
            INNER JOIN Skills s ON js.SkillId = s.SkillId
            WHERE LOWER(s.Name) IN @Skills
            AND j.IsActive = 1";

        var result = await conn.QueryAsync<JobListItem>(sql, new { Skills = skills.Select(s => s.ToLower()).ToList() });
        return result.ToList();
    }
}