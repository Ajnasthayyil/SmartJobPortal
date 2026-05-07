using SmartJobPortal.Application.Common;
using SmartJobPortal.Application.DTOs.NotificationDTOs;

namespace SmartJobPortal.Application.Interfaces;

public interface INotificationService
{
    // API & Internal use
    Task<ApiResponse<string>> CreateAsync(int userId, string title, string message, string type, string? jobTitle = null, string? companyName = null);

    Task<ApiResponse<List<NotificationResponse>>> GetUserNotificationsAsync(int userId);
    Task<ApiResponse<UnreadCountResponse>> GetUnreadCountAsync(int userId);
    Task<ApiResponse<string>> MarkAsReadAsync(int userId, int notificationId);
    Task<ApiResponse<string>> MarkAllAsReadAsync(int userId);
}