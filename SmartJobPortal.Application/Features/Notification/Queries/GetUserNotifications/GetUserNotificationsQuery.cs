using MediatR;
using SmartJobPortal.Application.Common;
using SmartJobPortal.Application.DTOs.NotificationDTOs;

namespace SmartJobPortal.Application.Features.Notification.Queries.GetUserNotifications;

public record GetUserNotificationsQuery(int UserId) : IRequest<ApiResponse<List<NotificationResponse>>>;
