using System;

namespace SmartJobPortal.Domain.Entities
{
    public class ResumeEducation
    {
        public long ResumeEducationId { get; set; }
        public long ParsedResumeId { get; set; }
        public string? Degree { get; set; }
        public string? Institution { get; set; }
        public string? GraduationYear { get; set; }

        public ParsedResume ParsedResume { get; set; } = null!;
    }
}
