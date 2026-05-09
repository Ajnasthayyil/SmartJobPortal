using Microsoft.AspNetCore.SignalR;
using SmartJobPortal.Application.Interfaces;
using SmartJobPortal.API.Hubs;

namespace SmartJobPortal.API.Services;

public class NotificationHubService : INotificationHubService
{
    private readonly IHubContext<NotificationHub> _hubContext;

    public NotificationHubService(IHubContext<NotificationHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public async Task SendNotificationAsync(int userId, object notification)
    {
        await _hubContext.Clients.User(userId.ToString()).SendAsync("ReceiveNotification", notification);
    }
}
