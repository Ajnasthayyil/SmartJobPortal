using MediatR;
using SmartJobPortal.Application.Common;
using SmartJobPortal.Application.DTOs.Admin;
using SmartJobPortal.Application.Interfaces;

namespace SmartJobPortal.Application.Features.Admin.Queries.GetDashboard;

public class GetAdminDashboardQueryHandler : IRequestHandler<GetAdminDashboardQuery, ApiResponse<AdminDashboardResponse>>
{
    private readonly IAdminRepository _adminRepo;
    private readonly ICacheService _cache;

    public GetAdminDashboardQueryHandler(IAdminRepository adminRepo, ICacheService cache)
    {
        _adminRepo = adminRepo;
        _cache = cache;
    }

    public async Task<ApiResponse<AdminDashboardResponse>> Handle(GetAdminDashboardQuery request, CancellationToken cancellationToken)
    {
        const string cacheKey = "admin:dashboard";
        var cached = await _cache.GetAsync<AdminDashboardResponse>(cacheKey);
        if (cached != null)
            return ApiResponse<AdminDashboardResponse>.Ok(cached);

        var stats = await _adminRepo.GetDashboardStatsAsync();
        await _cache.SetAsync(cacheKey, stats, TimeSpan.FromMinutes(5));

        return ApiResponse<AdminDashboardResponse>.Ok(stats);
    }
}
