using Dapper;
using SmartJobPortal.Application.DTOs.Candidate;
using SmartJobPortal.Application.Interfaces;
using SmartJobPortal.Domain.Entities;
using SmartJobPortal.Infrastructure.Data;

namespace SmartJobPortal.Infrastructure.Repositories;

public class ApplicationRepository : IApplicationRepository
{
    private readonly IDbConnectionFactory _factory;

    public ApplicationRepository(IDbConnectionFactory factory) => _factory = factory;

    public async Task<bool> AlreadyAppliedAsync(int candidateId, int jobId)
    {
        using var conn = _factory.CreateConnection();
        return await conn.ExecuteScalarAsync<bool>("""
            SELECT CAST(COUNT(1) AS BIT)
            FROM Applications
            WHERE CandidateId = @CandidateId AND JobId = @JobId
            """, new { CandidateId = candidateId, JobId = jobId });
    }

    public async Task<int> CreateAsync(SmartJobPortal.Domain.Entities.Application app)
    {
        using var conn = _factory.CreateConnection();
        return await conn.ExecuteScalarAsync<int>("""
            INSERT INTO Applications
                (CandidateId, JobId, Status, CoverNote, AppliedAt, UpdatedAt)
            OUTPUT INSERTED.ApplicationId
            VALUES
                (@CandidateId, @JobId, @Status, @CoverNote, @AppliedAt, @UpdatedAt)
            """, app);
    }

    public async Task<List<ApplicationTrackingResponse>> GetByCandidateIdAsync(int candidateId)
    {
        using var conn = _factory.CreateConnection();
        var rows = await conn.QueryAsync<ApplicationTrackingResponse>("""
            SELECT
                a.ApplicationId,
                a.JobId,
                j.Title      AS JobTitle,
                r.CompanyName,
                a.Status,
                a.AppliedAt,
                a.UpdatedAt
            FROM Applications a
            INNER JOIN Jobs      j ON j.JobId       = a.JobId
            INNER JOIN Recruiters r ON r.RecruiterId = j.RecruiterId
            WHERE a.CandidateId = @CandidateId
            ORDER BY a.AppliedAt DESC
            """, new { CandidateId = candidateId });
        return rows.ToList();
    }
}