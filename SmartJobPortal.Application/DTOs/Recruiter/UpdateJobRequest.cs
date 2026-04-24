using System.ComponentModel.DataAnnotations;

namespace SmartJobPortal.Application.DTOs.Recruiter;

public class UpdateJobRequest
{
    [Required(ErrorMessage = "Job title is required")]
    [StringLength(150)]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Description is required")]
    public string Description { get; set; } = string.Empty;

    [Required(ErrorMessage = "Location is required")]
    [StringLength(100)]
    public string Location { get; set; } = string.Empty;

    [Required(ErrorMessage = "Job type is required")]
    public string JobType { get; set; } = "FullTime";

    public int? MinSalary { get; set; }
    public int? MaxSalary { get; set; }

    [Range(0, 50)]
    public int MinExperienceYears { get; set; }

    public DateTime? ExpiresAt { get; set; }
    public bool IsActive { get; set; } = true;

    [Required(ErrorMessage = "At least one skill is required")]
    [MinLength(1)]
    public List<string> RequiredSkills { get; set; } = new();
}