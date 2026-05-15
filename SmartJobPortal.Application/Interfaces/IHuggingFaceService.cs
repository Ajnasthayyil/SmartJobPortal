using SmartJobPortal.Application.DTOs.Candidate;

namespace SmartJobPortal.Application.Interfaces;

public interface IHuggingFaceService
{
    Task<ResumeDto?> ExtractStructuredDataAsync(string text);
}