namespace SmartJobPortal.Domain.Entities;

public class Candidate
{
    public int CandidateId { get; set; }
    public int UserId { get; set; }
    public string Headline { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public int ExperienceYears { get; set; }
    public string? ResumeFilePath { get; set; }
    public string? ResumeOriginalName { get; set; }
    public DateTime? ResumeUploadedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
    public bool HasResume() => ResumeFilePath != null;
}