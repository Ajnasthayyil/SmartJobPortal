namespace SmartJobPortal.Domain.Entities;

public class Job
{
    public int JobId { get; set; }
    public int RecruiterId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string JobType { get; set; } = "FullTime";
    public int? MinSalary { get; set; }
    public int? MaxSalary { get; set; }
    public int MinExperienceYears { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime PostedAt { get; set; } = DateTime.Now;
    public DateTime? ExpiresAt { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.Now;

    // Joined fields
    public string CompanyName { get; set; } = string.Empty;
}