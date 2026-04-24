namespace SmartJobPortal.Application.DTOs.Candidate;

public class MatchScoreResponse
{
    public int JobId { get; set; }
    public string JobTitle { get; set; } = string.Empty;
    public decimal TotalScore { get; set; }
    public decimal SkillScore { get; set; }
    public decimal ExperienceScore { get; set; }
    public decimal LocationScore { get; set; }
    public List<string> MatchedSkills { get; set; } = new();
    public List<string> MissingSkills { get; set; } = new();

    public string ScoreLabel => TotalScore switch
    {
        >= 70 => "Strong Match",
        >= 40 => "Partial Match",
        _ => "Low Match"
    };
}