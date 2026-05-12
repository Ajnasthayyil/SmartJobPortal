using MediatR;
using SmartJobPortal.Application.Common;
using SmartJobPortal.Application.DTOs.Candidate;

namespace SmartJobPortal.Application.Features.Job.Queries.SearchJobs;

public record SearchJobsQuery(int UserId, JobSearchRequest Request) : IRequest<ApiResponse<JobSearchResponse>>;
