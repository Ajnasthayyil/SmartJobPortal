using System;

namespace SmartJobPortal.Domain.Entities
{
    public class ResumeSkill
    {
        public long ResumeSkillId { get; set; }
        public long ParsedResumeId { get; set; }
        public string SkillName { get; set; } = string.Empty;
        public string ProficiencyLevel { get; set; } = "Intermediate";
    }
}
