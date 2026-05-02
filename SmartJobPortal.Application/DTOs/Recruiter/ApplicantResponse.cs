namespace SmartJobPortal.Application.DTOs.Recruiter;

public class ApplicantResponse
{
    public int ApplicationId { get; set; }
    public int CandidateId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public int CandidateUserId { get; set; }
    public string Location { get; set; } = string.Empty;
    public int ExperienceYears { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? CoverNote { get; set; }
    public DateTime AppliedAt { get; set; }
    public bool HasResume { get; set; }
    public string? ResumeOriginalName { get; set; }
    public string? ResumeUrl { get; set; }
    public List<string> Skills { get; set; } = new();

    // AI score fields
    public decimal? TotalScore { get; set; }
    public decimal? SkillScore { get; set; }
    public decimal? ExperienceScore { get; set; }
    public decimal? LocationScore { get; set; }
    [System.Text.Json.Serialization.JsonIgnore]
    public string? MissingSkillsJson { get; set; }
    public List<string> MissingSkills { get; set; } = new();

    public string ScoreLabel => TotalScore switch
    {
        >= 70 => "Strong Match",
        >= 40 => "Partial Match",
        not null => "Low Match",
        _ => "Not Calculated"
    };
}