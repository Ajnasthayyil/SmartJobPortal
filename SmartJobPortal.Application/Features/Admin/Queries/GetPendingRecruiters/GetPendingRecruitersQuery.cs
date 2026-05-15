using MediatR;
using SmartJobPortal.Application.Common;
using SmartJobPortal.Application.DTOs.Admin;

namespace SmartJobPortal.Application.Features.Admin.Queries.GetPendingRecruiters;

public record GetPendingRecruitersQuery() : IRequest<ApiResponse<List<RecruiterApprovalResponse>>>;
