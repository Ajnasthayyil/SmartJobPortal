using SmartJobPortal.Application.Common;
using SmartJobPortal.Application.DTOs.NotificationDTOs;
using SmartJobPortal.Application.Interfaces;

namespace SmartJobPortal.Infrastructure.Services;

public class NotificationService : INotificationService
{
    private readonly INotificationRepository _repo;
    private readonly INotificationHubService _hubService;

    public NotificationService(INotificationRepository repo, INotificationHubService hubService)
    {
        _repo = repo;
        _hubService = hubService;
    }


    //  Create 
    public async Task<ApiResponse<string>> CreateAsync(
        int userId, string title, string message, string type, string? jobTitle = null, string? companyName = null)
    {
        try
        {
            int notificationId = await _repo.InsertAsync(userId, title, message, type, jobTitle, companyName);
            
            // Send real-time notification via SignalR
            var notification = new NotificationResponse
            {
                NotificationId = notificationId,
                Title = title,
                Message = message,
                Type = type,
                IsRead = false,
                CreatedAt = DateTime.UtcNow,
                JobTitle = jobTitle,
                CompanyName = companyName
            };

            await _hubService.SendNotificationAsync(userId, notification);

            return ApiResponse<string>.Ok("Notification sent.");


        }
        catch (Exception ex)
        {
            return ApiResponse<string>.Fail($"Failed to send notification: {ex.Message}", 500);
        }
    }

    //  Get all for user 
    public async Task<ApiResponse<List<NotificationResponse>>>
        GetUserNotificationsAsync(int userId)
    {
        var list = await _repo.GetByUserIdAsync(userId);
        return ApiResponse<List<NotificationResponse>>.Ok(list);
    }

    //  Get unread count 
    public async Task<ApiResponse<UnreadCountResponse>>
        GetUnreadCountAsync(int userId)
    {
        var count = await _repo.GetUnreadCountAsync(userId);
        return ApiResponse<UnreadCountResponse>.Ok(
            new UnreadCountResponse { Count = count });
    }

    //  Mark single as read 
    public async Task<ApiResponse<string>>
        MarkAsReadAsync(int userId, int notificationId)
    {
        await _repo.MarkAsReadAsync(userId, notificationId);
        return ApiResponse<string>.Ok("Marked as read.");
    }

    //  Mark all as read 
    public async Task<ApiResponse<string>>
        MarkAllAsReadAsync(int userId)
    {
        await _repo.MarkAllAsReadAsync(userId);
        return ApiResponse<string>.Ok("All notifications marked as read.");
    }
}