using System.ComponentModel.DataAnnotations;

namespace SmartJobPortal.Application.DTOs.Auth;

public class RegisterRequest
{
    
    [Required(ErrorMessage = "Full name is required")]
    [MinLength(3, ErrorMessage = "Full name must be at least 3 characters")]
    [RegularExpression(@"^[A-Za-z][A-Za-z\s]*$",
        ErrorMessage = "Full name must start with a letter and contain only alphabets")]
    public string? FullName { get; set; }

    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    [RegularExpression(@"^[^@\s]+@[^@\s]+\.[^@\s]+$",
        ErrorMessage = "Email format is not valid")]
    public string? Email { get; set; }

    [Required(ErrorMessage = "Password is required")]
    [MinLength(6, ErrorMessage = "Password must be at least 6 characters")]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[\W_]).+$",
        ErrorMessage = "Password must contain at least one uppercase, one lowercase, one digit, and one special character")]
    public string? Password { get; set; }

    [Required(ErrorMessage = "Phone number is required")]
    [RegularExpression(@"^[1-9][0-9]{9}$",
        ErrorMessage = "Phone number must be 10 digits and cannot start with 0")]
    public string? PhoneNumber { get; set; }

  
    [Required(ErrorMessage = "Role is required")]
    [RegularExpression("Admin|Recruiter|Candidate",
        ErrorMessage = "Role must be Admin, Recruiter, or Candidate")]
    public string? Role { get; set; }
}