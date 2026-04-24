using SmartJobPortal.Application.DTOs.Candidate;

namespace SmartJobPortal.Application.DTOs.Candidate;

public class ResumeParseResponse
{
    public ResumeDto? ParsedData { get; set; }
    public List<JobListItem> RecommendedJobs { get; set; } = new();
    public string Message { get; set; } = string.Empty;
}
