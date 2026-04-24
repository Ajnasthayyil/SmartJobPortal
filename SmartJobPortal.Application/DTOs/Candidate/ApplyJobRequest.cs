using System.ComponentModel.DataAnnotations;

namespace SmartJobPortal.Application.DTOs.Candidate;

public class ApplyJobRequest
{
    [Required(ErrorMessage = "JobId is required")]
    public int JobId { get; set; }

    [StringLength(500, ErrorMessage = "Cover note must not exceed 500 characters")]
    public string? CoverNote { get; set; }
}