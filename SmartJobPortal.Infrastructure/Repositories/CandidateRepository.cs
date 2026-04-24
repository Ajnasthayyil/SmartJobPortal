using Dapper;
using SmartJobPortal.Application.Interfaces;
using SmartJobPortal.Domain.Entities;
using SmartJobPortal.Infrastructure.Data;

namespace SmartJobPortal.Infrastructure.Repositories;

public class CandidateRepository : ICandidateRepository
{
    private readonly IDbConnectionFactory _factory;

    public CandidateRepository(IDbConnectionFactory factory) => _factory = factory;

    public async Task<Candidate?> GetByUserIdAsync(int userId)
    {
        using var conn = _factory.CreateConnection();
        return await conn.QuerySingleOrDefaultAsync<Candidate>(
            "SELECT * FROM Candidates WHERE UserId = @UserId",
            new { UserId = userId });
    }

    public async Task<int> UpsertAsync(Candidate c)
    {
        using var conn = _factory.CreateConnection();
        return await conn.ExecuteScalarAsync<int>("""
            MERGE Candidates AS target
            USING (SELECT @UserId AS UserId) AS source
                ON target.UserId = source.UserId
            WHEN MATCHED THEN
                UPDATE SET
                    Headline           = @Headline,
                    Summary            = @Summary,
                    Location           = @Location,
                    ExperienceYears    = @ExperienceYears,
                    UpdatedAt          = @UpdatedAt,
                    ResumeFilePath     = COALESCE(@ResumeFilePath,     ResumeFilePath),
                    ResumeOriginalName = COALESCE(@ResumeOriginalName, ResumeOriginalName),
                    ResumeUploadedAt   = COALESCE(@ResumeUploadedAt,   ResumeUploadedAt)
            WHEN NOT MATCHED THEN
                INSERT (UserId, Headline, Summary, Location, ExperienceYears,
                        ResumeFilePath, ResumeOriginalName, ResumeUploadedAt,
                        CreatedAt, UpdatedAt)
                VALUES (@UserId, @Headline, @Summary, @Location, @ExperienceYears,
                        @ResumeFilePath, @ResumeOriginalName, @ResumeUploadedAt,
                        @CreatedAt, @UpdatedAt)
            OUTPUT INSERTED.CandidateId;
            """, c);
    }

    public async Task<List<CandidateSkill>> GetSkillsAsync(int candidateId)
    {
        using var conn = _factory.CreateConnection();
        var rows = await conn.QueryAsync<CandidateSkill>("""
            SELECT
                cs.CandidateSkillId,
                cs.CandidateId,
                cs.SkillId,
                cs.Level,
                s.Name     AS SkillName,
                s.Category
            FROM CandidateSkills cs
            INNER JOIN Skills s ON s.SkillId = cs.SkillId
            WHERE cs.CandidateId = @CandidateId
            """, new { CandidateId = candidateId });
        return rows.ToList();
    }

    public async Task ReplaceSkillsAsync(int candidateId, List<CandidateSkill> skills)
    {
        using var conn = _factory.CreateConnection();
        conn.Open();
        using var tx = conn.BeginTransaction();

        await conn.ExecuteAsync(
            "DELETE FROM CandidateSkills WHERE CandidateId = @CandidateId",
            new { CandidateId = candidateId }, tx);

        if (skills.Any())
        {
            await conn.ExecuteAsync("""
                INSERT INTO CandidateSkills (CandidateId, SkillId, Level)
                VALUES (@CandidateId, @SkillId, @Level)
                """, skills, tx);
        }

        tx.Commit();
    }

    public async Task<int?> GetSkillIdByNameAsync(string skillName)
    {
        using var conn = _factory.CreateConnection();
        var id = await conn.ExecuteScalarAsync<int?>(
            "SELECT SkillId FROM Skills WHERE LOWER(Name) = LOWER(@Name)",
            new { Name = skillName });
        return id == 0 ? null : id;
    }

    public async Task<int> CreateSkillAsync(string skillName, string category = "General")
    {
        using var conn = _factory.CreateConnection();
        return await conn.ExecuteScalarAsync<int>("""
            INSERT INTO Skills (Name, Category)
            OUTPUT INSERTED.SkillId
            VALUES (@Name, @Category)
            """, new { Name = skillName, Category = category });
    }

    public async Task AddEducationAsync(List<CandidateEducation> education)
    {
        using var conn = _factory.CreateConnection();
        await conn.ExecuteAsync(@"
            INSERT INTO CandidateEducation (CandidateId, Degree, Institution, GraduationYear)
            VALUES (@CandidateId, @Degree, @Institution, @GraduationYear)", education);
    }

    public async Task AddExperienceAsync(List<CandidateExperience> experience)
    {
        using var conn = _factory.CreateConnection();
        await conn.ExecuteAsync(@"
            INSERT INTO CandidateExperience (CandidateId, Company, Role, Duration, Description)
            VALUES (@CandidateId, @Company, @Role, @Duration, @Description)", experience);
    }

    public async Task ClearEducationAndExperienceAsync(int candidateId)
    {
        using var conn = _factory.CreateConnection();
        await conn.ExecuteAsync("DELETE FROM CandidateEducation WHERE CandidateId = @CandidateId", new { CandidateId = candidateId });
        await conn.ExecuteAsync("DELETE FROM CandidateExperience WHERE CandidateId = @CandidateId", new { CandidateId = candidateId });
    }

    public async Task<List<CandidateEducation>> GetEducationAsync(int candidateId)
    {
        using var conn = _factory.CreateConnection();
        var rows = await conn.QueryAsync<CandidateEducation>("""
            SELECT EducationId, CandidateId, Degree, Institution, GraduationYear
            FROM CandidateEducation
            WHERE CandidateId = @CandidateId
            ORDER BY EducationId DESC
            """, new { CandidateId = candidateId });
        return rows.ToList();
    }

    public async Task<List<CandidateExperience>> GetExperienceAsync(int candidateId)
    {
        using var conn = _factory.CreateConnection();
        var rows = await conn.QueryAsync<CandidateExperience>("""
            SELECT ExperienceId, CandidateId, Company, Role, Duration, Description
            FROM CandidateExperience
            WHERE CandidateId = @CandidateId
            ORDER BY ExperienceId DESC
            """, new { CandidateId = candidateId });
        return rows.ToList();
    }
}