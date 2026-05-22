using MediatR;
using SmartJobPortal.Application.Common;
using SmartJobPortal.Application.DTOs.Admin;

namespace SmartJobPortal.Application.Features.Admin.Queries.GetAllRecruiters;

public record GetAllRecruitersQuery() : IRequest<ApiResponse<List<RecruiterApprovalResponse>>>;
