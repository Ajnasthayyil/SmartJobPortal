using SmartJobPortal.Application.DTOs.Candidate;

namespace SmartJobPortal.Application.Interfaces;

public interface IGeminiService
{
    Task<ResumeDto?> ExtractStructuredDataAsync(string sanitisedText);
    Task<string> GenerateSummaryAsync(List<string> skills, int experienceYears, string jobTitle);
}
