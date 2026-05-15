using MediatR;
using SmartJobPortal.Application.Common;
using SmartJobPortal.Application.DTOs.Admin;
using SmartJobPortal.Application.Interfaces;

namespace SmartJobPortal.Application.Features.Admin.Queries.GetAllJobs;

public class GetAllJobsQueryHandler : IRequestHandler<GetAllJobsQuery, ApiResponse<List<RecentJobActivity>>>
{
    private readonly IAdminRepository _adminRepo;

    public GetAllJobsQueryHandler(IAdminRepository adminRepo)
    {
        _adminRepo = adminRepo;
    }

    public async Task<ApiResponse<List<RecentJobActivity>>> Handle(GetAllJobsQuery request, CancellationToken cancellationToken)
    {
        var jobs = await _adminRepo.GetAllJobsAsync();
        return ApiResponse<List<RecentJobActivity>>.Ok(jobs);
    }
}
