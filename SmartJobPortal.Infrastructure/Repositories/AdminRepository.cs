using Dapper;
using SmartJobPortal.Application.DTOs.Admin;
using SmartJobPortal.Application.Interfaces;
using SmartJobPortal.Infrastructure.Data;

namespace SmartJobPortal.Infrastructure.Repositories;

public class AdminRepository : IAdminRepository
{
    private readonly IDbConnectionFactory _factory;

    public AdminRepository(IDbConnectionFactory factory) => _factory = factory;

    //  Users 

    public async Task<List<UserListResponse>> GetAllUsersAsync(
        string? roleFilter, bool? isActive)
    {
        using var conn = _factory.CreateConnection();

        var where = new List<string>();
        var p = new DynamicParameters();

        if (!string.IsNullOrWhiteSpace(roleFilter))
        {
            where.Add("r.RoleName = @RoleName");
            p.Add("RoleName", roleFilter);
        }

        if (isActive.HasValue)
        {
            where.Add("u.IsActive = @IsActive");
            p.Add("IsActive", isActive.Value ? 1 : 0);
        }

        var whereClause = where.Any()
            ? "WHERE " + string.Join(" AND ", where)
            : string.Empty;

        var sql = $"""
            SELECT
                u.UserId,
                u.FullName,
                u.Email,
                u.PhoneNumber,
                r.RoleName,
                u.RoleId,
                u.IsActive,
                u.IsApproved,
                u.CreatedAt
            FROM Users u
            INNER JOIN Roles r ON r.RoleId = u.RoleId
            {whereClause}
            ORDER BY u.CreatedAt DESC
            """;

        var users = await conn.QueryAsync<UserListResponse>(sql, p);
        return users.ToList();
    }

    public async Task<UserListResponse?> GetUserByIdAsync(int userId)
    {
        using var conn = _factory.CreateConnection();
        return await conn.QuerySingleOrDefaultAsync<UserListResponse>("""
            SELECT
                u.UserId,
                u.FullName,
                u.Email,
                u.PhoneNumber,
                r.RoleName,
                u.RoleId,
                u.IsActive,
                u.IsApproved,
                u.CreatedAt
            FROM Users u
            INNER JOIN Roles r ON r.RoleId = u.RoleId
            WHERE u.UserId = @UserId
            """, new { UserId = userId });
    }

    public async Task<bool> BlockUserAsync(int userId)
    {
        using var conn = _factory.CreateConnection();
        var rows = await conn.ExecuteAsync("""
            UPDATE Users
            SET IsActive = 0
            WHERE UserId = @UserId
            """, new { UserId = userId });
        return rows > 0;
    }

    public async Task<bool> UnblockUserAsync(int userId)
    {
        using var conn = _factory.CreateConnection();
        var rows = await conn.ExecuteAsync("""
            UPDATE Users
            SET IsActive = 1
            WHERE UserId = @UserId
            """, new { UserId = userId });
        return rows > 0;
    }

    //  Recruiter approvals 

    public async Task<List<RecruiterApprovalResponse>> GetPendingRecruitersAsync()
    {
        using var conn = _factory.CreateConnection();
        var rows = await conn.QueryAsync<RecruiterApprovalResponse>("""
            SELECT
                u.UserId,
                u.FullName,
                u.Email,
                u.PhoneNumber,
                u.IsApproved,
                u.IsActive,
                u.CreatedAt AS RegisteredAt,
                COALESCE(rec.RecruiterId, 0)    AS RecruiterId,
                COALESCE(rec.CompanyName, '')   AS CompanyName,
                rec.Industry,
                rec.Location,
                rec.Website
            FROM Users u
            LEFT JOIN Recruiters rec ON rec.UserId = u.UserId
            INNER JOIN Roles r ON r.RoleId = u.RoleId
            WHERE r.RoleName   = 'Recruiter'
              AND u.IsApproved = 0
              AND u.IsActive   = 1
            ORDER BY u.CreatedAt DESC
            """);
        return rows.ToList();
    }

    public async Task<List<RecruiterApprovalResponse>> GetAllRecruitersAsync()
    {
        using var conn = _factory.CreateConnection();
        var rows = await conn.QueryAsync<RecruiterApprovalResponse>("""
            SELECT
                u.UserId,
                u.FullName,
                u.Email,
                u.PhoneNumber,
                u.IsApproved,
                u.IsActive,
                u.CreatedAt AS RegisteredAt,
                COALESCE(rec.RecruiterId, 0)    AS RecruiterId,
                COALESCE(rec.CompanyName, '')   AS CompanyName,
                rec.Industry,
                rec.Location,
                rec.Website
            FROM Users u
            LEFT JOIN Recruiters rec ON rec.UserId = u.UserId
            INNER JOIN Roles r ON r.RoleId = u.RoleId
            WHERE r.RoleName = 'Recruiter'
            ORDER BY u.CreatedAt DESC
            """);
        return rows.ToList();
    }

    public async Task<RecruiterApprovalResponse?> GetRecruiterApprovalByUserIdAsync(
        int userId)
    {
        using var conn = _factory.CreateConnection();
        return await conn.QuerySingleOrDefaultAsync<RecruiterApprovalResponse>("""
            SELECT
                u.UserId,
                u.FullName,
                u.Email,
                u.PhoneNumber,
                u.IsApproved,
                u.IsActive,
                u.CreatedAt AS RegisteredAt,
                COALESCE(rec.RecruiterId, 0)    AS RecruiterId,
                COALESCE(rec.CompanyName, '')   AS CompanyName,
                rec.Industry,
                rec.Location,
                rec.Website
            FROM Users u
            LEFT JOIN Recruiters rec ON rec.UserId = u.UserId
            INNER JOIN Roles r ON r.RoleId = u.RoleId
            WHERE u.UserId   = @UserId
              AND r.RoleName = 'Recruiter'
            """, new { UserId = userId });
    }

    public async Task<bool> ApproveRecruiterAsync(int userId)
    {
        using var conn = _factory.CreateConnection();
        var rows = await conn.ExecuteAsync("""
            UPDATE Users
            SET IsApproved = 1,
                IsActive   = 1
            WHERE UserId = @UserId
            """, new { UserId = userId });
        return rows > 0;
    }

    public async Task<bool> RejectRecruiterAsync(int userId)
    {
        using var conn = _factory.CreateConnection();
        // Reject = block the account (IsActive = 0, IsApproved stays 0)
        var rows = await conn.ExecuteAsync("""
            UPDATE Users
            SET IsApproved = 0,
                IsActive   = 0
            WHERE UserId = @UserId
            """, new { UserId = userId });
        return rows > 0;
    }

    //  Job monitoring 

    public async Task<List<RecentJobActivity>> GetAllJobsAsync()
    {
        using var conn = _factory.CreateConnection();
        var rows = await conn.QueryAsync<RecentJobActivity>("""
            SELECT
                j.JobId,
                j.Title,
                r.CompanyName,
                j.Location,
                j.PostedAt,
                (SELECT COUNT(*)
                 FROM Applications a
                 WHERE a.JobId = j.JobId) AS TotalApplicants
            FROM Jobs j
            INNER JOIN Recruiters r ON r.RecruiterId = j.RecruiterId
            ORDER BY j.PostedAt DESC
            """);
        return rows.ToList();
    }

    public async Task<bool> DeactivateJobAsync(int jobId)
    {
        using var conn = _factory.CreateConnection();
        var rows = await conn.ExecuteAsync("""
            UPDATE Jobs
            SET IsActive  = 0,
                UpdatedAt = GETDATE()
            WHERE JobId = @JobId
            """, new { JobId = jobId });
        return rows > 0;
    }

    //  Dashboard stats 

    public async Task<AdminDashboardResponse> GetDashboardStatsAsync()
    {
        using var conn = _factory.CreateConnection();

        // Single multi-result query — one round trip to DB
        using var multi = await conn.QueryMultipleAsync("""
            -- User stats
            SELECT
                COUNT(*)                                       AS TotalUsers,
                SUM(CASE WHEN r.RoleName = 'Candidate'
                         THEN 1 ELSE 0 END)                   AS TotalCandidates,
                SUM(CASE WHEN r.RoleName = 'Recruiter'
                         THEN 1 ELSE 0 END)                   AS TotalRecruiters,
                SUM(CASE WHEN r.RoleName = 'Recruiter'
                          AND u.IsApproved = 0
                          AND u.IsActive   = 1
                         THEN 1 ELSE 0 END)                   AS PendingRecruiterApprovals,
                SUM(CASE WHEN u.IsActive = 0
                         THEN 1 ELSE 0 END)                   AS BlockedUsers
            FROM Users u
            INNER JOIN Roles r ON r.RoleId = u.RoleId;

            -- Job stats
            SELECT
                COUNT(*)                                       AS TotalJobs,
                SUM(CASE WHEN IsActive = 1 THEN 1 ELSE 0 END) AS ActiveJobs,
                SUM(CASE WHEN IsActive = 0 THEN 1 ELSE 0 END) AS InactiveJobs
            FROM Jobs;

            -- Application stats
            SELECT
                COUNT(*)                                       AS TotalApplications,
                SUM(CASE WHEN CAST(AppliedAt AS DATE)
                              = CAST(GETDATE() AS DATE)
                         THEN 1 ELSE 0 END)                   AS ApplicationsToday
            FROM Applications;

            -- Recent 5 users
            SELECT TOP 5
                u.UserId,
                u.FullName,
                u.Email,
                r.RoleName,
                u.CreatedAt
            FROM Users u
            INNER JOIN Roles r ON r.RoleId = u.RoleId
            ORDER BY u.CreatedAt DESC;

            -- Recent 5 jobs
            SELECT TOP 5
                j.JobId,
                j.Title,
                rec.CompanyName,
                j.Location,
                j.PostedAt,
                (SELECT COUNT(*) FROM Applications a
                 WHERE a.JobId = j.JobId) AS TotalApplicants
            FROM Jobs j
            INNER JOIN Recruiters rec ON rec.RecruiterId = j.RecruiterId
            ORDER BY j.PostedAt DESC;
            """);

        var dashboard = new AdminDashboardResponse();

        // Read each result set in order
        var userStats = await multi.ReadSingleAsync();
        dashboard.TotalUsers = (int)userStats.TotalUsers;
        dashboard.TotalCandidates = (int)userStats.TotalCandidates;
        dashboard.TotalRecruiters = (int)userStats.TotalRecruiters;
        dashboard.PendingRecruiterApprovals = (int)userStats.PendingRecruiterApprovals;
        dashboard.BlockedUsers = (int)userStats.BlockedUsers;

        var jobStats = await multi.ReadSingleAsync();
        dashboard.TotalJobs = (int)jobStats.TotalJobs;
        dashboard.ActiveJobs = (int)jobStats.ActiveJobs;
        dashboard.InactiveJobs = (int)jobStats.InactiveJobs;

        var appStats = await multi.ReadSingleAsync();
        dashboard.TotalApplications = (int)appStats.TotalApplications;
        dashboard.ApplicationsToday = (int)appStats.ApplicationsToday;

        dashboard.RecentUsers = (await multi.ReadAsync<RecentUserActivity>()).ToList();
        dashboard.RecentJobs = (await multi.ReadAsync<RecentJobActivity>()).ToList();

        return dashboard;
    }
}