using System.ComponentModel.DataAnnotations;

namespace SmartJobPortal.Application.DTOs.Candidate;

public class CandidateProfileRequest
{
    [Required(ErrorMessage = "Headline is required")]
    [StringLength(150, ErrorMessage = "Headline must not exceed 150 characters")]
    public string Headline { get; set; } = string.Empty;

    [StringLength(1000, ErrorMessage = "Summary must not exceed 1000 characters")]
    public string Summary { get; set; } = string.Empty;

    [Required(ErrorMessage = "Location is required")]
    [StringLength(100, ErrorMessage = "Location must not exceed 100 characters")]
    public string Location { get; set; } = string.Empty;

    [Range(0, 50, ErrorMessage = "Experience years must be between 0 and 50")]
    public int ExperienceYears { get; set; }

    public List<SkillRequest> Skills { get; set; } = new();
}

public class SkillRequest
{
    [Required(ErrorMessage = "Skill name is required")]
    [StringLength(100)]
    public string SkillName { get; set; } = string.Empty;

    public string Level { get; set; } = "Intermediate"; // Beginner | Intermediate | Expert
}