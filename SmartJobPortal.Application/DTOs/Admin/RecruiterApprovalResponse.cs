namespace SmartJobPortal.Application.DTOs.Admin;

public class RecruiterApprovalResponse
{
    public int UserId { get; set; }
    public int RecruiterId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public string? Industry { get; set; }
    public string? Location { get; set; }
    public string? Website { get; set; }
    public bool IsApproved { get; set; }
    public bool IsActive { get; set; }
    public DateTime RegisteredAt { get; set; }
}