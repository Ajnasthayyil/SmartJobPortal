namespace SmartJobPortal.Application.DTOs.Recruiter;

public class RecruiterProfileResponse
{
    public int RecruiterId { get; set; }
    public int UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public string? Website { get; set; }
    public string? Industry { get; set; }
    public string? Description { get; set; }
    public string? Location { get; set; }
    public DateTime CreatedAt { get; set; }
    public int TotalJobsPosted { get; set; }
}