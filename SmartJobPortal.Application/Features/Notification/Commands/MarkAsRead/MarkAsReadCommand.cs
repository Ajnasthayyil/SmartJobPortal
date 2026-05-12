using MediatR;
using SmartJobPortal.Application.Common;

namespace SmartJobPortal.Application.Features.Notification.Commands.MarkAsRead;

public record MarkAsReadCommand(int UserId, int NotificationId) : IRequest<ApiResponse<string>>;
