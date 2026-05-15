using MediatR;
using SmartJobPortal.Application.Common;
using SmartJobPortal.Application.DTOs.Recruiter;

namespace SmartJobPortal.Application.Features.Recruiter.Queries.GetApplicants;

public record GetApplicantsQuery(int UserId, int JobId) : IRequest<ApiResponse<List<ApplicantResponse>>>;
