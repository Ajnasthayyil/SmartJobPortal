using Dapper;
using SmartJobPortal.Application.Interfaces;
using SmartJobPortal.Domain.Entities;
using SmartJobPortal.Infrastructure.Data;

namespace SmartJobPortal.Infrastructure.Repositories;

public class RecruiterRepository : IRecruiterRepository
{
    private readonly IDbConnectionFactory _factory;

    public RecruiterRepository(IDbConnectionFactory factory) => _factory = factory;

    public async Task<Recruiter?> GetByUserIdAsync(int userId)
    {
        using var conn = _factory.CreateConnection();
        return await conn.QuerySingleOrDefaultAsync<Recruiter>("""
            SELECT
                r.*,
                u.FullName,
                u.Email
            FROM Recruiters r
            INNER JOIN Users u ON u.UserId = r.UserId
            WHERE r.UserId = @UserId
            """, new { UserId = userId });
    }

    public async Task<int> UpsertAsync(Recruiter r)
    {
        using var conn = _factory.CreateConnection();
        return await conn.ExecuteScalarAsync<int>("""
            MERGE Recruiters AS target
            USING (SELECT @UserId AS UserId) AS source
                ON target.UserId = source.UserId
            WHEN MATCHED THEN
                UPDATE SET
                    CompanyName = @CompanyName,
                    Website     = @Website,
                    Industry    = @Industry,
                    Description = @Description,
                    Location    = @Location,
                    UpdatedAt   = @UpdatedAt
            WHEN NOT MATCHED THEN
                INSERT (UserId, CompanyName, Website, Industry,
                        Description, Location, CreatedAt, UpdatedAt)
                VALUES (@UserId, @CompanyName, @Website, @Industry,
                        @Description, @Location, @CreatedAt, @UpdatedAt)
            OUTPUT INSERTED.RecruiterId;
            """, r);
    }

    public async Task<int> GetTotalJobsPostedAsync(int recruiterId)
    {
        using var conn = _factory.CreateConnection();
        return await conn.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM Jobs WHERE RecruiterId = @RecruiterId",
            new { RecruiterId = recruiterId });
    }
}