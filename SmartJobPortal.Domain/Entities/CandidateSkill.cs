namespace SmartJobPortal.Domain.Entities;

public class CandidateSkill
{
    public int CandidateSkillId { get; set; }
    public int CandidateId { get; set; }
    public int SkillId { get; set; }
    public string Level { get; set; } = "Intermediate";

    // Joined from Skills table — populated by queries
    public string SkillName { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
}