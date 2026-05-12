using MediatR;
using SmartJobPortal.Application.Common;
using SmartJobPortal.Application.DTOs.NotificationDTOs;
using SmartJobPortal.Application.Interfaces;

namespace SmartJobPortal.Application.Features.Notification.Commands.CreateNotification;

public class CreateNotificationCommandHandler : IRequestHandler<CreateNotificationCommand, ApiResponse<string>>
{
    private readonly INotificationRepository _repo;
    private readonly INotificationHubService _hubService;

    public CreateNotificationCommandHandler(INotificationRepository repo, INotificationHubService hubService)
    {
        _repo = repo;
        _hubService = hubService;
    }

    public async Task<ApiResponse<string>> Handle(CreateNotificationCommand request, CancellationToken cancellationToken)
    {
        try
        {
            int notificationId = await _repo.InsertAsync(
                request.UserId, 
                request.Title, 
                request.Message, 
                request.Type, 
                request.JobTitle, 
                request.CompanyName);
            
            var notification = new NotificationResponse
            {
                NotificationId = notificationId,
                Title = request.Title,
                Message = request.Message,
                Type = request.Type,
                IsRead = false,
                CreatedAt = DateTime.UtcNow,
                JobTitle = request.JobTitle,
                CompanyName = request.CompanyName
            };

            await _hubService.SendNotificationAsync(request.UserId, notification);

            return ApiResponse<string>.Ok("Notification sent.");
        }
        catch (Exception ex)
        {
            return ApiResponse<string>.Fail($"Failed to send notification: {ex.Message}", 500);
        }
    }
}
