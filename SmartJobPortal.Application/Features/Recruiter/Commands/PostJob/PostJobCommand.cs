using MediatR;
using SmartJobPortal.Application.Common;
using SmartJobPortal.Application.DTOs.Recruiter;

namespace SmartJobPortal.Application.Features.Recruiter.Commands.PostJob;

public record PostJobCommand(int UserId, PostJobRequest Request) : IRequest<ApiResponse<JobResponse>>;
