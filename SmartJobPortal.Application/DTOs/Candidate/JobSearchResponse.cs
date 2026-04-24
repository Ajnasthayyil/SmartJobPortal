namespace SmartJobPortal.Application.DTOs.Candidate;

public class JobSearchResponse
{
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    public List<JobListItem> Jobs { get; set; } = new();
}

public class JobListItem
{
    public int JobId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string JobType { get; set; } = string.Empty;
    public int? MinSalary { get; set; }
    public int? MaxSalary { get; set; }
    public int MinExperienceYears { get; set; }
    public List<string> RequiredSkills { get; set; } = new();
    public decimal? MatchScore { get; set; }
    public DateTime PostedAt { get; set; }
}

public class JobDetail : JobListItem
{
    public string Description { get; set; } = string.Empty;
    public DateTime? ExpiresAt { get; set; }
    public MatchScoreResponse? MatchScore { get; set; }
    public new decimal? MatchScoreValue { get; set; }
}