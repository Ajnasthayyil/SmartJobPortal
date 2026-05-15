using MediatR;
using SmartJobPortal.Application.Common;
using SmartJobPortal.Application.DTOs.Recruiter;

namespace SmartJobPortal.Application.Features.Recruiter.Commands.UpdateJob;

public record UpdateRecruiterJobCommand(int UserId, int JobId, UpdateJobRequest Request) : IRequest<ApiResponse<string>>;
