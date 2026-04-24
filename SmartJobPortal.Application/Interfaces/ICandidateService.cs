using SmartJobPortal.Application.Common;
using SmartJobPortal.Application.DTOs.Candidate;

namespace SmartJobPortal.Application.Interfaces;

public interface ICandidateService
{
    Task<ApiResponse<CandidateProfileResponse>> GetProfileAsync(int userId);
    Task<ApiResponse<CandidateProfileResponse>> UpsertProfileAsync(int userId,
        CandidateProfileRequest request);
}