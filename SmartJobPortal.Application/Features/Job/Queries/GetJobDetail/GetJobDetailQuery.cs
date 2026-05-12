using MediatR;
using SmartJobPortal.Application.Common;
using SmartJobPortal.Application.DTOs.Candidate;
using SmartJobPortal.Domain.Entities;

namespace SmartJobPortal.Application.Features.Job.Queries.GetJobDetail;

public record GetJobDetailQuery(int UserId, int JobId) : IRequest<ApiResponse<JobDetail>>;
