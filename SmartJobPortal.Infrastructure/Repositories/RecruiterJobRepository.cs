using System.Text.Json;
using Dapper;
using SmartJobPortal.Application.DTOs.Recruiter;
using SmartJobPortal.Application.Interfaces;
using SmartJobPortal.Domain.Entities;
using SmartJobPortal.Infrastructure.Data;

namespace SmartJobPortal.Infrastructure.Repositories;

public class RecruiterJobRepository : IRecruiterJobRepository
{
    private readonly IDbConnectionFactory _factory;

    public RecruiterJobRepository(IDbConnectionFactory factory) => _factory = factory;

    //  Jobs 

    public async Task<int> CreateJobAsync(Job job)
    {
        using var conn = _factory.CreateConnection();
        return await conn.ExecuteScalarAsync<int>("""
            INSERT INTO Jobs
                (RecruiterId, Title, Description, Location, JobType,
                 MinSalary, MaxSalary, MinExperienceYears, IsActive,
                 PostedAt, ExpiresAt, UpdatedAt)
            OUTPUT INSERTED.JobId
            VALUES
                (@RecruiterId, @Title, @Description, @Location, @JobType,
                 @MinSalary, @MaxSalary, @MinExperienceYears, @IsActive,
                 @PostedAt, @ExpiresAt, @UpdatedAt)
            """, job);
    }

    public async Task<bool> UpdateJobAsync(Job job)
    {
        using var conn = _factory.CreateConnection();
        var rows = await conn.ExecuteAsync("""
            UPDATE Jobs SET
                Title              = @Title,
                Description        = @Description,
                Location           = @Location,
                JobType            = @JobType,
                MinSalary          = @MinSalary,
                MaxSalary          = @MaxSalary,
                MinExperienceYears = @MinExperienceYears,
                IsActive           = @IsActive,
                ExpiresAt          = @ExpiresAt,
                UpdatedAt          = @UpdatedAt
            WHERE JobId = @JobId
            """, job);
        return rows > 0;
    }

    public async Task<bool> SoftDeleteJobAsync(int jobId, int recruiterId)
    {
        using var conn = _factory.CreateConnection();
        var rows = await conn.ExecuteAsync("""
            UPDATE Jobs
            SET IsActive  = 0,
                UpdatedAt = GETDATE()
            WHERE JobId = @JobId
              AND RecruiterId = @RecruiterId
            """, new { JobId = jobId, RecruiterId = recruiterId });
        return rows > 0;
    }

    public async Task<bool> ToggleJobStatusAsync(int jobId, int recruiterId)
    {
        using var conn = _factory.CreateConnection();
        var rows = await conn.ExecuteAsync("""
            UPDATE Jobs
            SET IsActive  = CASE WHEN IsActive = 1 THEN 0 ELSE 1 END,
                UpdatedAt = GETDATE()
            WHERE JobId = @JobId
              AND RecruiterId = @RecruiterId
            """, new { JobId = jobId, RecruiterId = recruiterId });
        return rows > 0;
    }

    public async Task<Job?> GetJobByIdAsync(int jobId)
    {
        using var conn = _factory.CreateConnection();
        return await conn.QuerySingleOrDefaultAsync<Job>("""
            SELECT
                j.*,
                r.CompanyName
            FROM Jobs j
            INNER JOIN Recruiters r ON r.RecruiterId = j.RecruiterId
            WHERE j.JobId = @JobId
            """, new { JobId = jobId });
    }

    public async Task<List<JobResponse>> GetJobsByRecruiterIdAsync(int recruiterId)
    {
        using var conn = _factory.CreateConnection();

        // Get all jobs for this recruiter
        var jobs = (await conn.QueryAsync<JobResponse>("""
            SELECT
                j.JobId, j.RecruiterId, j.Title, j.Description,
                j.Location, j.JobType, j.MinSalary, j.MaxSalary,
                j.MinExperienceYears, j.IsActive, j.PostedAt, j.ExpiresAt,
                (SELECT COUNT(*) FROM Applications a
                 WHERE a.JobId = j.JobId) AS TotalApplicants
            FROM Jobs j
            WHERE j.RecruiterId = @RecruiterId
            ORDER BY j.PostedAt DESC
            """, new { RecruiterId = recruiterId })).ToList();

        // Attach required skills to each job
        foreach (var job in jobs)
        {
            job.RequiredSkills = await GetSkillNamesByJobIdAsync(job.JobId);
        }

        return jobs;
    }

    public async Task<bool> JobBelongsToRecruiterAsync(int jobId, int recruiterId)
    {
        using var conn = _factory.CreateConnection();
        return await conn.ExecuteScalarAsync<bool>("""
            SELECT CAST(COUNT(1) AS BIT)
            FROM Jobs
            WHERE JobId = @JobId AND RecruiterId = @RecruiterId
            """, new { JobId = jobId, RecruiterId = recruiterId });
    }

    //  Job Skills 

    public async Task ReplaceJobSkillsAsync(int jobId, List<int> skillIds)
    {
        using var conn = _factory.CreateConnection();
        conn.Open();
        using var tx = conn.BeginTransaction();

        await conn.ExecuteAsync(
            "DELETE FROM JobSkills WHERE JobId = @JobId",
            new { JobId = jobId }, tx);

        if (skillIds.Any())
        {
            var rows = skillIds.Select(sid => new { JobId = jobId, SkillId = sid });
            await conn.ExecuteAsync("""
                INSERT INTO JobSkills (JobId, SkillId)
                VALUES (@JobId, @SkillId)
                """, rows, tx);
        }

        tx.Commit();
    }

    // ── Applicants ────────────────────────────────────────────────────────────

    public async Task<List<ApplicantResponse>> GetApplicantsAsync(int jobId)
    {
        using var conn = _factory.CreateConnection();

        var applicants = (await conn.QueryAsync<ApplicantResponse>("""
            SELECT
                a.ApplicationId,
                c.CandidateId,
                u.FullName,
                u.Email,
                u.UserId AS CandidateUserId,
                c.Location,
                c.ExperienceYears,
                a.Status,
                a.CoverNote,
                a.AppliedAt,
                CASE WHEN c.ResumeFilePath IS NOT NULL THEN 1 ELSE 0 END AS HasResume,
                c.ResumeOriginalName,
                -- Match score fields (LEFT JOIN — may be null if not calculated yet)
                ms.TotalScore,
                ms.SkillScore,
                ms.ExperienceScore,
                ms.LocationScore,
                ms.MissingSkills
            FROM Applications a
            INNER JOIN Candidates c ON c.CandidateId = a.CandidateId
            INNER JOIN Users      u ON u.UserId       = c.UserId
            LEFT  JOIN MatchScores ms
                ON ms.CandidateId = c.CandidateId
               AND ms.JobId       = a.JobId
            WHERE a.JobId = @JobId
            ORDER BY a.AppliedAt DESC
            """, new { JobId = jobId })).ToList();

        // Attach candidate skills + deserialise MissingSkills JSON
        foreach (var applicant in applicants)
        {
            applicant.Skills = await GetCandidateSkillNamesAsync(applicant.CandidateId);

            // MissingSkills comes from DB as a JSON string — deserialise it
            if (!string.IsNullOrEmpty(
                    await GetMissingSkillsJsonAsync(applicant.CandidateId, jobId)))
            {
                var json = await GetMissingSkillsJsonAsync(applicant.CandidateId, jobId);
                applicant.MissingSkills =
                    JsonSerializer.Deserialize<List<string>>(json ?? "[]") ?? new();
            }
        }

        return applicants;
    }

    public async Task<int> GetTotalApplicantsAsync(int jobId)
    {
        using var conn = _factory.CreateConnection();
        return await conn.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM Applications WHERE JobId = @JobId",
            new { JobId = jobId });
    }

    //  Application status 

    public async Task<bool> UpdateApplicationStatusAsync(
        int applicationId, int recruiterId, string status)
    {
        using var conn = _factory.CreateConnection();
        // Verify the application belongs to a job owned by this recruiter
        var rows = await conn.ExecuteAsync("""
            UPDATE a
            SET a.Status    = @Status,
                a.UpdatedAt = GETDATE()
            FROM Applications a
            INNER JOIN Jobs j
                ON j.JobId = a.JobId
            INNER JOIN Recruiters r
                ON r.RecruiterId = j.RecruiterId
            WHERE a.ApplicationId = @ApplicationId
              AND r.RecruiterId   = @RecruiterId
            """, new
        {
            ApplicationId = applicationId,
            RecruiterId = recruiterId,
            Status = status
        });
        return rows > 0;
    }

    //  Private helpers 

    private async Task<List<string>> GetSkillNamesByJobIdAsync(int jobId)
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

    private async Task<List<string>> GetCandidateSkillNamesAsync(int candidateId)
    {
        using var conn = _factory.CreateConnection();
        var names = await conn.QueryAsync<string>("""
            SELECT s.Name
            FROM CandidateSkills cs
            INNER JOIN Skills s ON s.SkillId = cs.SkillId
            WHERE cs.CandidateId = @CandidateId
            """, new { CandidateId = candidateId });
        return names.ToList();
    }

    private async Task<string?> GetMissingSkillsJsonAsync(int candidateId, int jobId)
    {
        using var conn = _factory.CreateConnection();
        return await conn.ExecuteScalarAsync<string?>("""
            SELECT MissingSkills
            FROM MatchScores
            WHERE CandidateId = @CandidateId AND JobId = @JobId
            """, new { CandidateId = candidateId, JobId = jobId });
    }
}