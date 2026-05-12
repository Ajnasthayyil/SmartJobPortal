using MediatR;
using SmartJobPortal.Application.Common;
using SmartJobPortal.Application.DTOs.Candidate;

namespace SmartJobPortal.Application.Features.Job.Commands.ApplyJob;

public record ApplyJobCommand(int UserId, ApplyJobRequest Request) : IRequest<ApiResponse<int>>;
