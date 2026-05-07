using SmartJobPortal.Application.Common;
using SmartJobPortal.Application.DTOs.NotificationDTOs;
using SmartJobPortal.Application.Interfaces;

namespace SmartJobPortal.Application.Services;

public class NotificationService : INotificationService
{
    private readonly INotificationRepository _repo;

    public NotificationService(INotificationRepository repo)
        => _repo = repo;

    // ── Create ─────────────────────────────────────────────────────
    public async Task<ApiResponse<string>> CreateAsync(
        int userId, string title, string message, string type, string? jobTitle = null, string? companyName = null)
    {
        try
        {
            await _repo.InsertAsync(userId, title, message, type, jobTitle, companyName);
            return ApiResponse<string>.Ok("Notification sent.");
        }
        catch (Exception ex)
        {
            return ApiResponse<string>.Fail($"Failed to send notification: {ex.Message}", 500);
        }
    }

    // ── Get all for user ─────────────────────────────────────────
    public async Task<ApiResponse<List<NotificationResponse>>>
        GetUserNotificationsAsync(int userId)
    {
        var list = await _repo.GetByUserIdAsync(userId);
        return ApiResponse<List<NotificationResponse>>.Ok(list);
    }

    // ── Get unread count ─────────────────────────────────────────
    public async Task<ApiResponse<UnreadCountResponse>>
        GetUnreadCountAsync(int userId)
    {
        var count = await _repo.GetUnreadCountAsync(userId);
        return ApiResponse<UnreadCountResponse>.Ok(
            new UnreadCountResponse { Count = count });
    }

    // ── Mark single as read ──────────────────────────────────────
    public async Task<ApiResponse<string>>
        MarkAsReadAsync(int userId, int notificationId)
    {
        await _repo.MarkAsReadAsync(userId, notificationId);
        return ApiResponse<string>.Ok("Marked as read.");
    }

    // ── Mark all as read ─────────────────────────────────────────
    public async Task<ApiResponse<string>>
        MarkAllAsReadAsync(int userId)
    {
        await _repo.MarkAllAsReadAsync(userId);
        return ApiResponse<string>.Ok("All notifications marked as read.");
    }
}