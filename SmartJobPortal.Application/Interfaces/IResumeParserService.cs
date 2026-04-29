using SmartJobPortal.Application.DTOs.Candidate;

namespace SmartJobPortal.Application.Interfaces;

public interface IResumeParserService
{
    Task<ResumeDto?> ParseResumeAsync(string resumeText);
}
