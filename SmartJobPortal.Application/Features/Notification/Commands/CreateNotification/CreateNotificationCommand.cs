using MediatR;
using SmartJobPortal.Application.Common;

namespace SmartJobPortal.Application.Features.Notification.Commands.CreateNotification;

public record CreateNotificationCommand(
    int UserId, 
    string Title, 
    string Message, 
    string Type, 
    string? JobTitle = null, 
    string? CompanyName = null) : IRequest<ApiResponse<string>>;
