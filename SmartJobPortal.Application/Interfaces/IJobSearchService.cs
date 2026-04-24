using SmartJobPortal.Application.Common;
using SmartJobPortal.Application.DTOs.Candidate;

namespace SmartJobPortal.Application.Interfaces;

public interface IJobSearchService
{
    Task<ApiResponse<JobSearchResponse>> SearchAsync(int userId, JobSearchRequest request);
    Task<ApiResponse<JobDetail>> GetDetailAsync(int userId, int jobId);
    Task<ApiResponse<int>> ApplyAsync(int userId, ApplyJobRequest request);
    Task<ApiResponse<List<ApplicationTrackingResponse>>> GetMyApplicationsAsync(int userId);
    Task<List<JobListItem>> RecommendJobsBySkillsAsync(int userId, List<string> skills);
}