namespace SmartJobPortal.Domain.Entities;

public class CandidateExperience
{
    public int ExperienceId { get; set; }
    public int CandidateId { get; set; }
    public string Company { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string? Duration { get; set; }
    public string? Description { get; set; }
}
