using SmartJobPortal.Application.Common;
using SmartJobPortal.Application.DTOs.Recruiter;

namespace SmartJobPortal.Application.Interfaces;

public interface IRecruiterService
{
    // Profile
    Task<ApiResponse<RecruiterProfileResponse>> GetProfileAsync(int userId);
    Task<ApiResponse<RecruiterProfileResponse>> UpsertProfileAsync(
        int userId, RecruiterProfileRequest request);

    // Jobs
    Task<ApiResponse<JobResponse>> PostJobAsync(int userId, PostJobRequest request);
    Task<ApiResponse<List<JobResponse>>> GetMyJobsAsync(int userId);
    Task<ApiResponse<JobResponse>> GetJobDetailAsync(int userId, int jobId);
    Task<ApiResponse<JobResponse>> UpdateJobAsync(int userId, int jobId,
        UpdateJobRequest request);
    Task<ApiResponse<string>> DeleteJobAsync(int userId, int jobId);

    // Applicants
    Task<ApiResponse<List<ApplicantResponse>>> GetApplicantsAsync(int userId, int jobId);
    Task<ApiResponse<List<ApplicantResponse>>> GetRankedApplicantsAsync(int userId, int jobId);
    Task<ApiResponse<string>> UpdateApplicationStatusAsync(int userId,
        int applicationId, UpdateStatusRequest request);
}