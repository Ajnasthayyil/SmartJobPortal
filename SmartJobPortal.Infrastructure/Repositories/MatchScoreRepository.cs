using Dapper;
using SmartJobPortal.Application.Interfaces;
using SmartJobPortal.Domain.Entities;
using SmartJobPortal.Infrastructure.Data;

namespace SmartJobPortal.Infrastructure.Repositories;

public class MatchScoreRepository : IMatchScoreRepository
{
    private readonly IDbConnectionFactory _factory;

    public MatchScoreRepository(IDbConnectionFactory factory) => _factory = factory;

    public async Task<MatchScore?> GetAsync(int candidateId, int jobId)
    {
        using var conn = _factory.CreateConnection();
        return await conn.QuerySingleOrDefaultAsync<MatchScore>("""
            SELECT * FROM MatchScores
            WHERE CandidateId = @CandidateId AND JobId = @JobId
            """, new { CandidateId = candidateId, JobId = jobId });
    }

    public async Task UpsertAsync(MatchScore score)
    {
        using var conn = _factory.CreateConnection();
        await conn.ExecuteAsync("""
            MERGE MatchScores AS target
            USING (SELECT @CandidateId AS CandidateId,
                          @JobId       AS JobId) AS source
                ON target.CandidateId = source.CandidateId
               AND target.JobId       = source.JobId
            WHEN MATCHED THEN
                UPDATE SET
                    SkillScore      = @SkillScore,
                    ExperienceScore = @ExperienceScore,
                    LocationScore   = @LocationScore,
                    TotalScore      = @TotalScore,
                    MissingSkills   = @MissingSkills,
                    CalculatedAt    = @CalculatedAt
            WHEN NOT MATCHED THEN
                INSERT (CandidateId, JobId, SkillScore, ExperienceScore,
                        LocationScore, TotalScore, MissingSkills, CalculatedAt)
                VALUES (@CandidateId, @JobId, @SkillScore, @ExperienceScore,
                        @LocationScore, @TotalScore, @MissingSkills, @CalculatedAt);
            """, score);
    }
}