using System.ComponentModel.DataAnnotations;

namespace SmartJobPortal.Application.DTOs.Recruiter;

public class PostJobRequest
{
    [Required(ErrorMessage = "Job title is required")]
    [StringLength(150, ErrorMessage = "Title must not exceed 150 characters")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Job description is required")]
    public string Description { get; set; } = string.Empty;

    [Required(ErrorMessage = "Location is required")]
    [StringLength(100)]
    public string Location { get; set; } = string.Empty;

    [Required(ErrorMessage = "Job type is required")]
    public string JobType { get; set; } = "FullTime";
    // FullTime | PartTime | Contract | Remote

    [Range(0, int.MaxValue, ErrorMessage = "Minimum salary must be positive")]
    public int? MinSalary { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "Maximum salary must be positive")]
    public int? MaxSalary { get; set; }

    [Range(0, 50, ErrorMessage = "Experience must be between 0 and 50")]
    public int MinExperienceYears { get; set; }

    public DateTime? ExpiresAt { get; set; }

    [Required(ErrorMessage = "At least one required skill must be provided")]
    [MinLength(1, ErrorMessage = "At least one skill is required")]
    public List<string> RequiredSkills { get; set; } = new();
}