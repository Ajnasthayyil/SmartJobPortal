using MediatR;
using SmartJobPortal.Application.Common;
using SmartJobPortal.Application.DTOs.Admin;
using SmartJobPortal.Application.Interfaces;

namespace SmartJobPortal.Application.Features.Admin.Queries.GetPendingRecruiters;

public class GetPendingRecruitersQueryHandler : IRequestHandler<GetPendingRecruitersQuery, ApiResponse<List<RecruiterApprovalResponse>>>
{
    private readonly IAdminRepository _adminRepo;

    public GetPendingRecruitersQueryHandler(IAdminRepository adminRepo)
    {
        _adminRepo = adminRepo;
    }

    public async Task<ApiResponse<List<RecruiterApprovalResponse>>> Handle(GetPendingRecruitersQuery request, CancellationToken cancellationToken)
    {
        var recruiters = await _adminRepo.GetPendingRecruitersAsync();
        return ApiResponse<List<RecruiterApprovalResponse>>.Ok(recruiters);
    }
}
