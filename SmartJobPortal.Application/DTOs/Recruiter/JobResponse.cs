namespace SmartJobPortal.Application.DTOs.Recruiter;

public class JobResponse
{
    public int JobId { get; set; }
    public int RecruiterId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string JobType { get; set; } = string.Empty;
    public int? MinSalary { get; set; }
    public int? MaxSalary { get; set; }
    public int MinExperienceYears { get; set; }
    public bool IsActive { get; set; }
    public DateTime PostedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public List<string> RequiredSkills { get; set; } = new();
    public int TotalApplicants { get; set; }
}