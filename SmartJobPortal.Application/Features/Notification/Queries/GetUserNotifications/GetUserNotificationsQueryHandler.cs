using MediatR;
using SmartJobPortal.Application.Common;
using SmartJobPortal.Application.DTOs.NotificationDTOs;
using SmartJobPortal.Application.Interfaces;

namespace SmartJobPortal.Application.Features.Notification.Queries.GetUserNotifications;

public class GetUserNotificationsQueryHandler : IRequestHandler<GetUserNotificationsQuery, ApiResponse<List<NotificationResponse>>>
{
    private readonly INotificationRepository _repo;

    public GetUserNotificationsQueryHandler(INotificationRepository repo)
    {
        _repo = repo;
    }

    public async Task<ApiResponse<List<NotificationResponse>>> Handle(GetUserNotificationsQuery request, CancellationToken cancellationToken)
    {
        var list = await _repo.GetByUserIdAsync(request.UserId);
        return ApiResponse<List<NotificationResponse>>.Ok(list);
    }
}
