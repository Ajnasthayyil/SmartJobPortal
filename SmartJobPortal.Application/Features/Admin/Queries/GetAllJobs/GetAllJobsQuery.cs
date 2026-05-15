using MediatR;
using SmartJobPortal.Application.Common;
using SmartJobPortal.Application.DTOs.Admin;

namespace SmartJobPortal.Application.Features.Admin.Queries.GetAllJobs;

public record GetAllJobsQuery() : IRequest<ApiResponse<List<RecentJobActivity>>>;
