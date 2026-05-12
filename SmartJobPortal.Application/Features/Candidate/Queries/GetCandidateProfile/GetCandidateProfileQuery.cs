using MediatR;
using SmartJobPortal.Application.Common;
using SmartJobPortal.Application.DTOs.Candidate;

namespace SmartJobPortal.Application.Features.Candidate.Queries.GetCandidateProfile;

public record GetCandidateProfileQuery(int UserId) : IRequest<ApiResponse<CandidateProfileResponse>>;
