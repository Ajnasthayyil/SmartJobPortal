namespace SmartJobPortal.Domain.Entities;

public class Skill
{
    public int SkillId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = "General";
}