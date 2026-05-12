using MediatR;
using SmartJobPortal.Application.Common;
using SmartJobPortal.Application.DTOs.Recruiter;

namespace SmartJobPortal.Application.Features.Recruiter.Commands.UpdateApplicationStatus;

public record UpdateApplicationStatusCommand(int UserId, int ApplicationId, UpdateStatusRequest Request) : IRequest<ApiResponse<string>>;
