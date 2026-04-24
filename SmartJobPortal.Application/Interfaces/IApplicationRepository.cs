using SmartJobPortal.Application.DTOs.Candidate;
using SmartJobPortal.Domain.Entities;

namespace SmartJobPortal.Application.Interfaces;

public interface IApplicationRepository
{
    Task<bool> AlreadyAppliedAsync(int candidateId, int jobId);
    Task<int> CreateAsync(SmartJobPortal.Domain.Entities.Application application);
    Task<List<ApplicationTrackingResponse>> GetByCandidateIdAsync(int candidateId);
}