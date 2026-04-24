namespace SmartJobPortal.Application.DTOs.Candidate;

public class ApplicationTrackingResponse
{
    public int ApplicationId { get; set; }
    public int JobId { get; set; }
    public string JobTitle { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public decimal? MatchScore { get; set; }
    public DateTime AppliedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<StatusTimelineItem> Timeline { get; set; } = new();
}

public class StatusTimelineItem
{
    public string Status { get; set; } = string.Empty;
    public bool IsCompleted { get; set; }
    public bool IsCurrent { get; set; }
    public DateTime? OccurredAt { get; set; }
}