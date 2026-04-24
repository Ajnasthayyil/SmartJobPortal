namespace SmartJobPortal.Application.DTOs.Candidate;

public class ResumeDto
{
    public string? FullName { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public List<string> Skills { get; set; } = new();
    public double TotalExperience { get; set; }
    public List<EducationDto> Education { get; set; } = new();
    public List<ExperienceDto> WorkExperience { get; set; } = new();
}

public class EducationDto
{
    public string? Degree { get; set; }
    public string? Institution { get; set; }
    public string? Year { get; set; }
}

public class ExperienceDto
{
    public string? Company { get; set; }
    public string? Role { get; set; }
    public string? Duration { get; set; }
    public string? Description { get; set; }
}
