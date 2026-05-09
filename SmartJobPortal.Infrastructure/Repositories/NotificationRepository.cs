using Dapper;
using SmartJobPortal.Application.DTOs.NotificationDTOs;
using SmartJobPortal.Application.Interfaces;
using SmartJobPortal.Infrastructure.Data;

namespace SmartJobPortal.Infrastructure.Repositories;

public class NotificationRepository : INotificationRepository
{
    private readonly IDbConnectionFactory _factory;
    private static bool _schemaChecked = false;

    public NotificationRepository(IDbConnectionFactory factory)
        => _factory = factory;

    private async Task EnsureSchemaAsync()
    {
        if (_schemaChecked) return;
        using var conn = _factory.CreateConnection();
        // Add JobId to link notifications to jobs
        try { await conn.ExecuteAsync("ALTER TABLE Notifications ADD JobId INT NULL"); } catch {}
        
        // Ensure JobTitle and CompanyName columns also exist for legacy/manual notifications
        try { await conn.ExecuteAsync("ALTER TABLE Notifications ADD JobTitle NVARCHAR(200) NULL"); } catch {}
        try { await conn.ExecuteAsync("ALTER TABLE Notifications ADD CompanyName NVARCHAR(200) NULL"); } catch {}
        
        _schemaChecked = true;
    }

    public async Task<int> InsertAsync(
        int userId, string title,
        string message, string type,
        string? jobTitle = null, string? companyName = null)

    {
        await EnsureSchemaAsync();
        using var conn = _factory.CreateConnection();
        
        // If we have jobTitle/companyName, let's try to find a JobId if it's missing (optional logic)
        // But for now, we'll just insert what we have.
        
        return await conn.ExecuteScalarAsync<int>("""
            INSERT INTO Notifications
                (UserId, Title, Message, Type, IsRead, CreatedAt, JobTitle, CompanyName)
            VALUES
                (@UserId, @Title, @Message, @Type, 0, GETDATE(), @JobTitle, @CompanyName);
            SELECT SCOPE_IDENTITY();
            """, new
        {
            UserId = userId,
            Title = title,
            Message = message,
            Type = type,
            JobTitle = jobTitle,
            CompanyName = companyName
        });

    }

    public async Task<List<NotificationResponse>>
        GetByUserIdAsync(int userId, int limit = 20)
    {
        await EnsureSchemaAsync();
        using var conn = _factory.CreateConnection();
        
        // We join with Jobs and Recruiters to get the LATEST info, 
        // but fallback to the stored JobTitle/CompanyName if the job is gone or it's a custom notif.
        
        var rows = await conn.QueryAsync<NotificationResponse>("""
            SELECT TOP (@Limit)
                n.NotificationId, n.Title, n.Message, n.Type, n.IsRead, n.CreatedAt,
                COALESCE(j.Title, n.JobTitle) as JobTitle,
                COALESCE(r.CompanyName, n.CompanyName) as CompanyName
            FROM Notifications n
            LEFT JOIN Jobs j ON j.Title = n.JobTitle -- Temporary join logic since we don't have JobId filled yet
            LEFT JOIN Recruiters r ON r.CompanyName = n.CompanyName
            WHERE n.UserId = @UserId
            ORDER BY n.CreatedAt DESC
            """, new { UserId = userId, Limit = limit });
            
        return rows.ToList();
    }

    public async Task<int> GetUnreadCountAsync(int userId)
    {
        using var conn = _factory.CreateConnection();
        return await conn.ExecuteScalarAsync<int>("""
            SELECT COUNT(*)
            FROM Notifications
            WHERE UserId  = @UserId
              AND IsRead  = 0
            """, new { UserId = userId });
    }

    public async Task MarkAsReadAsync(int userId, int notificationId)
    {
        using var conn = _factory.CreateConnection();
        await conn.ExecuteAsync("""
            UPDATE Notifications
            SET IsRead = 1
            WHERE NotificationId = @NotificationId
              AND UserId         = @UserId
            """, new
        {
            NotificationId = notificationId,
            UserId = userId
        });
    }

    public async Task MarkAllAsReadAsync(int userId)
    {
        using var conn = _factory.CreateConnection();
        await conn.ExecuteAsync("""
            UPDATE Notifications
            SET IsRead = 1
            WHERE UserId = @UserId
              AND IsRead = 0
            """, new { UserId = userId });
    }
}