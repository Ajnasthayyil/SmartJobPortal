namespace SmartJobPortal.Domain.Entities;

public class MatchScore
{
    public int MatchScoreId { get; set; }
    public int CandidateId { get; set; }
    public int JobId { get; set; }
    public decimal SkillScore { get; set; }
    public decimal ExperienceScore { get; set; }
    public decimal LocationScore { get; set; }
    public decimal TotalScore { get; set; }
    public string MissingSkills { get; set; } = "[]"; // JSON array stored as string
    public DateTime CalculatedAt { get; set; } = DateTime.Now;
}