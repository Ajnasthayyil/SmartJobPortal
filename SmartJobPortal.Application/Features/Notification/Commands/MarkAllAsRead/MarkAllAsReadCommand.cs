using MediatR;
using SmartJobPortal.Application.Common;

namespace SmartJobPortal.Application.Features.Notification.Commands.MarkAllAsRead;

public record MarkAllAsReadCommand(int UserId) : IRequest<ApiResponse<string>>;
