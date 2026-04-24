using SmartJobPortal.Application.DTOs.Recruiter;
using SmartJobPortal.Domain.Entities;

namespace SmartJobPortal.Application.Interfaces;

public interface IRecruiterRepository
{
    Task<Recruiter?> GetByUserIdAsync(int userId);
    Task<int> UpsertAsync(Recruiter recruiter);
    Task<int> GetTotalJobsPostedAsync(int recruiterId);
}