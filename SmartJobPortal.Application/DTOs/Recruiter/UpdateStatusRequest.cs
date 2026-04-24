using System.ComponentModel.DataAnnotations;

namespace SmartJobPortal.Application.DTOs.Recruiter;

public class UpdateStatusRequest
{
    [Required(ErrorMessage = "Status is required")]
    public string Status { get; set; } = string.Empty;
    // Applied | UnderReview | Shortlisted | Interview | Offered | Rejected
}