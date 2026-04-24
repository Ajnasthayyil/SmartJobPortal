using SmartJobPortal.Application.DTOs.Candidate;

namespace SmartJobPortal.Application.Interfaces;

public interface IJobRepository
{
    Task<(List<JobListItem> jobs, int totalCount)> SearchAsync(JobSearchRequest request);
    Task<JobDetail?> GetDetailAsync(int jobId);
    Task<List<string>> GetSkillNamesAsync(int jobId);
    Task<int> GetMinExperienceAsync(int jobId);
    Task<string?> GetLocationAsync(int jobId);
    Task<List<JobListItem>> GetBySkillsAsync(List<string> skills);
}