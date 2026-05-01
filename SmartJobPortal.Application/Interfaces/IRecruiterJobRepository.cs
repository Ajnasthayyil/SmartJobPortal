using SmartJobPortal.Application.DTOs.Recruiter;
using SmartJobPortal.Domain.Entities;

namespace SmartJobPortal.Application.Interfaces;

public interface IRecruiterJobRepository
{
    // Jobs
    Task<int> CreateJobAsync(Job job);
    Task<bool> UpdateJobAsync(Job job);
    Task<bool> SoftDeleteJobAsync(int jobId, int recruiterId);
    Task<bool> ToggleJobStatusAsync(int jobId, int recruiterId);
    Task<Job?> GetJobByIdAsync(int jobId);
    Task<List<JobResponse>> GetJobsByRecruiterIdAsync(int recruiterId);
    Task<bool> JobBelongsToRecruiterAsync(int jobId, int recruiterId);

    // Job Skills
    Task ReplaceJobSkillsAsync(int jobId, List<int> skillIds);

    // Applicants
    Task<List<ApplicantResponse>> GetApplicantsAsync(int jobId);
    Task<int> GetTotalApplicantsAsync(int jobId);

    // Application status
    Task<bool> UpdateApplicationStatusAsync(int applicationId,
        int recruiterId, string status);
}