using MediatR;
using SmartJobPortal.Application.Common;
using SmartJobPortal.Application.DTOs.Recruiter;

namespace SmartJobPortal.Application.Features.Recruiter.Commands.UpdateProfile;

public record UpdateRecruiterProfileCommand(int UserId, RecruiterProfileRequest Request) : IRequest<ApiResponse<RecruiterProfileResponse>>;
