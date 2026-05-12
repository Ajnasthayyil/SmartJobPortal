using MediatR;
using SmartJobPortal.Application.Common;
using SmartJobPortal.Application.Interfaces;

namespace SmartJobPortal.Application.Features.Notification.Commands.MarkAllAsRead;

public class MarkAllAsReadCommandHandler : IRequestHandler<MarkAllAsReadCommand, ApiResponse<string>>
{
    private readonly INotificationRepository _repo;

    public MarkAllAsReadCommandHandler(INotificationRepository repo)
    {
        _repo = repo;
    }

    public async Task<ApiResponse<string>> Handle(MarkAllAsReadCommand request, CancellationToken cancellationToken)
    {
        await _repo.MarkAllAsReadAsync(request.UserId);
        return ApiResponse<string>.Ok("All notifications marked as read.");
    }
}
