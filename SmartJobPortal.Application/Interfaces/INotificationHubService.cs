namespace SmartJobPortal.Application.Interfaces;

public interface INotificationHubService
{
    Task SendNotificationAsync(int userId, object notification);
}
