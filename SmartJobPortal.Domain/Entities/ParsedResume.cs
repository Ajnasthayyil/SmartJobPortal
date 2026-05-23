using System;
using System.Collections.Generic;

namespace SmartJobPortal.Domain.Entities
{
    public class ParsedResume
    {
        public long ParsedResumeId { get; set; }
        public long CandidateId { get; set; }
        public string? FileName { get; set; }
        public string FilePath { get; set; } = string.Empty;
        public string Status { get; set; } = "Pending";
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public string? ParsedJson { get; set; }
        public string? ErrorMessage { get; set; }

        public ICollection<ResumeSkill> Skills { get; set; } = new List<ResumeSkill>();
        public ICollection<ResumeExperience> Experiences { get; set; } = new List<ResumeExperience>();
        public ICollection<ResumeEducation> Educations { get; set; } = new List<ResumeEducation>();
    }
}
