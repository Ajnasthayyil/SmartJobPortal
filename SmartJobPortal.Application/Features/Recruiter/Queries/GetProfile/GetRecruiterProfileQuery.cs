using MediatR;
using SmartJobPortal.Application.Common;
using SmartJobPortal.Application.DTOs.Recruiter;

namespace SmartJobPortal.Application.Features.Recruiter.Queries.GetProfile;

public record GetRecruiterProfileQuery(int UserId) : IRequest<ApiResponse<RecruiterProfileResponse>>;
