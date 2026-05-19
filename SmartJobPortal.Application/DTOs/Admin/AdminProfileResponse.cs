namespace SmartJobPortal.Application.DTOs.Admin;

public class AdminProfileResponse
{
    public int UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string? ProfilePictureUrl { get; set; }
    public DateTime CreatedAt { get; set; }
}
