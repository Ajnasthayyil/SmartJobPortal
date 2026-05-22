using MediatR;
using SmartJobPortal.Application.Common;
using SmartJobPortal.Application.DTOs.Admin;
using SmartJobPortal.Application.Interfaces;

namespace SmartJobPortal.Application.Features.Admin.Queries.GetAllRecruiters;

public class GetAllRecruitersQueryHandler : IRequestHandler<GetAllRecruitersQuery, ApiResponse<List<RecruiterApprovalResponse>>>
{
    private readonly IAdminRepository _adminRepo;

    public GetAllRecruitersQueryHandler(IAdminRepository adminRepo)
    {
        _adminRepo = adminRepo;
    }

    public async Task<ApiResponse<List<RecruiterApprovalResponse>>> Handle(GetAllRecruitersQuery request, CancellationToken cancellationToken)
    {
        var recruiters = await _adminRepo.GetAllRecruitersAsync();
        return ApiResponse<List<RecruiterApprovalResponse>>.Ok(recruiters);
    }
}
