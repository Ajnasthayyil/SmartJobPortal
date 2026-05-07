namespace SmartJobPortal.Application.DTOs.NotificationDTOs;

public class SendNotificationRequest
{
    public int TargetUserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}
