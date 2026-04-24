using System.ComponentModel.DataAnnotations;

namespace SmartJobPortal.Application.DTOs.Recruiter;

public class RecruiterProfileRequest
{
    [Required(ErrorMessage = "Company name is required")]
    [StringLength(150, ErrorMessage = "Company name must not exceed 150 characters")]
    public string CompanyName { get; set; } = string.Empty;

    [StringLength(255, ErrorMessage = "Website must not exceed 255 characters")]
    public string? Website { get; set; }

    [StringLength(100, ErrorMessage = "Industry must not exceed 100 characters")]
    public string? Industry { get; set; }

    [StringLength(1000, ErrorMessage = "Description must not exceed 1000 characters")]
    public string? Description { get; set; }

    [StringLength(100, ErrorMessage = "Location must not exceed 100 characters")]
    public string? Location { get; set; }
}