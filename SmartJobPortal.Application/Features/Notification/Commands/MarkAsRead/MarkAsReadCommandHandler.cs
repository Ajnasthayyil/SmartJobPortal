using MediatR;
using SmartJobPortal.Application.Common;
using SmartJobPortal.Application.Interfaces;

namespace SmartJobPortal.Application.Features.Notification.Commands.MarkAsRead;

public class MarkAsReadCommandHandler : IRequestHandler<MarkAsReadCommand, ApiResponse<string>>
{
    private readonly INotificationRepository _repo;

    public MarkAsReadCommandHandler(INotificationRepository repo)
    {
        _repo = repo;
    }

    public async Task<ApiResponse<string>> Handle(MarkAsReadCommand request, CancellationToken cancellationToken)
    {
        await _repo.MarkAsReadAsync(request.UserId, request.NotificationId);
        return ApiResponse<string>.Ok("Marked as read.");
    }
}
