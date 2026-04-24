using System.ComponentModel.DataAnnotations;

namespace SmartJobPortal.Application.DTOs.Auth;

public class LoginRequest
{
    [Required]
    [EmailAddress]
    public string? Email { get; set; }

    [Required]
    public string? Password { get; set; }
}