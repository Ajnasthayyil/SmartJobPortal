namespace SmartJobPortal.Application.DTOs.Candidate;

public class CandidateProfileResponse
{
    public int CandidateId { get; set; }
    public int UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string Headline { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public int ExperienceYears { get; set; }
    public bool HasResume { get; set; }
    public string? ResumeOriginalName { get; set; }
    public DateTime? ResumeUploadedAt { get; set; }
    public List<SkillResponse> Skills { get; set; } = new();
    public List<EducationResponse> Education { get; set; } = new();
    public List<ExperienceResponse> WorkExperience { get; set; } = new();
}

public class SkillResponse
{
    public int SkillId { get; set; }
    public string SkillName { get; set; } = string.Empty;
    public string Level { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
}

public class EducationResponse
{
    public int EducationId { get; set; }
    public string Degree { get; set; } = string.Empty;
    public string Institution { get; set; } = string.Empty;
    public string? GraduationYear { get; set; }
}

public class ExperienceResponse
{
    public int ExperienceId { get; set; }
    public string Company { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string? Duration { get; set; }
    public string? Description { get; set; }
}