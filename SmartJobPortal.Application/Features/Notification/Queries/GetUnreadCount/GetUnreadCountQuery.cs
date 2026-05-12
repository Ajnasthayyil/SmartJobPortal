using MediatR;
using SmartJobPortal.Application.Common;
using SmartJobPortal.Application.DTOs.NotificationDTOs;

namespace SmartJobPortal.Application.Features.Notification.Queries.GetUnreadCount;

public record GetUnreadCountQuery(int UserId) : IRequest<ApiResponse<UnreadCountResponse>>;
