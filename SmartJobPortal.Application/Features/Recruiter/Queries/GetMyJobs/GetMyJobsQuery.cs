using MediatR;
using SmartJobPortal.Application.Common;
using SmartJobPortal.Application.DTOs.Recruiter;

namespace SmartJobPortal.Application.Features.Recruiter.Queries.GetMyJobs;

public record GetMyJobsQuery(int UserId) : IRequest<ApiResponse<List<JobResponse>>>;
