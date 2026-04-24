namespace SmartJobPortal.Domain.Entities;

public class CandidateEducation
{
    public int EducationId { get; set; }
    public int CandidateId { get; set; }
    public string Degree { get; set; } = string.Empty;
    public string Institution { get; set; } = string.Empty;
    public string? GraduationYear { get; set; }
}
