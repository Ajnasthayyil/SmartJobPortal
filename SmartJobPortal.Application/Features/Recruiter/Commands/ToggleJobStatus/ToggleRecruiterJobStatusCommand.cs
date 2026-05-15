using MediatR;
using SmartJobPortal.Application.Common;

namespace SmartJobPortal.Application.Features.Recruiter.Commands.ToggleJobStatus;

public record ToggleRecruiterJobStatusCommand(int UserId, int JobId) : IRequest<ApiResponse<string>>;
