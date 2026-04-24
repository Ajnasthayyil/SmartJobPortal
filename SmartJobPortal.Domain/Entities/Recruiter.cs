namespace SmartJobPortal.Domain.Entities;

public class Recruiter
{
    public int RecruiterId { get; set; }
    public int UserId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string? Website { get; set; }
    public string? Industry { get; set; }
    public string? Description { get; set; }
    public string? Location { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;

    // Joined from Users table
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}