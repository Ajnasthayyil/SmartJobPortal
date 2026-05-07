using SmartJobPortal.Application.DTOs.NotificationDTOs;

namespace SmartJobPortal.Application.Interfaces;

public interface INotificationRepository
{
    Task InsertAsync(int userId, string title,
                     string message, string type,
                     string? jobTitle = null, string? companyName = null);

    Task<List<NotificationResponse>>
        GetByUserIdAsync(int userId, int limit = 20);

    Task<int> GetUnreadCountAsync(int userId);

    Task MarkAsReadAsync(int userId, int notificationId);

    Task MarkAllAsReadAsync(int userId);
}