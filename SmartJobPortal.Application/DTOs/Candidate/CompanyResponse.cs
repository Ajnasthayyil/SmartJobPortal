namespace SmartJobPortal.Application.DTOs.Candidate;

public class CompanyResponse
{
    public int RecruiterId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string? Industry { get; set; }
    public string? Location { get; set; }
    public string? Website { get; set; }
    public string? Description { get; set; }
    public int OpenJobsCount { get; set; }
}
