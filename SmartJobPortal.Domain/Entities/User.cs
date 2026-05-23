namespace SmartJobPortal.Domain.Entities;

public class User
{
    public int UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public int RoleId { get; set; }
    public bool IsActive { get; set; }
    public bool IsApproved { get; set; }
    public DateTime CreatedAt { get; set; }
    public string RoleName { get; set; } = string.Empty;
    public string? ProfilePictureUrl { get; set; }
    public string? ResetToken { get; set; }
    public DateTime? ResetTokenExpiry { get; set; }
}