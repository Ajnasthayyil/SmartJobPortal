using MediatR;
using SmartJobPortal.Application.Common;
using SmartJobPortal.Application.DTOs.NotificationDTOs;
using SmartJobPortal.Application.Interfaces;

namespace SmartJobPortal.Application.Features.Notification.Queries.GetUnreadCount;

public class GetUnreadCountQueryHandler : IRequestHandler<GetUnreadCountQuery, ApiResponse<UnreadCountResponse>>
{
    private readonly INotificationRepository _repo;

    public GetUnreadCountQueryHandler(INotificationRepository repo)
    {
        _repo = repo;
    }

    public async Task<ApiResponse<UnreadCountResponse>> Handle(GetUnreadCountQuery request, CancellationToken cancellationToken)
    {
        var count = await _repo.GetUnreadCountAsync(request.UserId);
        return ApiResponse<UnreadCountResponse>.Ok(new UnreadCountResponse { Count = count });
    }
}
