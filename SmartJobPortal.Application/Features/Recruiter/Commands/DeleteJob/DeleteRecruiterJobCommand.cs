using MediatR;
using SmartJobPortal.Application.Common;

namespace SmartJobPortal.Application.Features.Recruiter.Commands.DeleteJob;

public record DeleteRecruiterJobCommand(int UserId, int JobId) : IRequest<ApiResponse<string>>;
