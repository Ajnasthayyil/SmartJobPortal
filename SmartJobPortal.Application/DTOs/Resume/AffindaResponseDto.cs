using System.Collections.Generic;
using SmartJobPortal.Application.DTOs.Candidate;

namespace SmartJobPortal.Application.DTOs.Resume
{
    public class AffindaResponseDto
    {
        public string? FullName { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public List<string> Skills { get; set; } = new();
        public List<EducationDto> Education { get; set; } = new();
        public List<ExperienceDto> WorkExperience { get; set; } = new();
    }
}
