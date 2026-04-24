namespace SmartJobPortal.Domain.Entities;

public class Application
{
    public int ApplicationId { get; set; }
    public int CandidateId { get; set; }
    public int JobId { get; set; }
    public string Status { get; set; } = "Applied";
    public string? CoverNote { get; set; }
    public DateTime AppliedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
}