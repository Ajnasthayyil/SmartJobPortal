using MediatR;
using SmartJobPortal.Application.Common;
using SmartJobPortal.Application.DTOs.Recruiter;

namespace SmartJobPortal.Application.Features.Recruiter.Queries.GetRankedApplicants;

public record GetRankedApplicantsQuery(int UserId, int JobId) : IRequest<ApiResponse<List<ApplicantResponse>>>;
