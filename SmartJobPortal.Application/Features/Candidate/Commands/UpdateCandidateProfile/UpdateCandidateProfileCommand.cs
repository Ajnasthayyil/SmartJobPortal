using MediatR;
using SmartJobPortal.Application.Common;
using SmartJobPortal.Application.DTOs.Candidate;

namespace SmartJobPortal.Application.Features.Candidate.Commands.UpdateCandidateProfile;

public record UpdateCandidateProfileCommand(int UserId, CandidateProfileRequest Request) 
    : IRequest<ApiResponse<CandidateProfileResponse>>;
