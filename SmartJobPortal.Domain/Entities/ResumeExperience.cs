using System;

namespace SmartJobPortal.Domain.Entities
{
    public class ResumeExperience
    {
        public long ResumeExperienceId { get; set; }
        public long ParsedResumeId { get; set; }
        public string? Company { get; set; }
        public string? Role { get; set; }
        public string? Duration { get; set; }
        public string? Description { get; set; }

        public ParsedResume ParsedResume { get; set; } = null!;
    }
}
