using MediatR;
using SmartJobPortal.Application.Common;
using SmartJobPortal.Application.DTOs.Recruiter;

namespace SmartJobPortal.Application.Features.Recruiter.Queries.GetJobDetail;

public record GetRecruiterJobDetailQuery(int UserId, int JobId) : IRequest<ApiResponse<JobResponse>>;
