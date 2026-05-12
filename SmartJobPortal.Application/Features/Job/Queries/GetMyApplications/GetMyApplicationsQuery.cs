using MediatR;
using SmartJobPortal.Application.Common;
using SmartJobPortal.Application.DTOs.Candidate;

namespace SmartJobPortal.Application.Features.Job.Queries.GetMyApplications;

public record GetMyApplicationsQuery(int UserId) : IRequest<ApiResponse<List<ApplicationTrackingResponse>>>;
