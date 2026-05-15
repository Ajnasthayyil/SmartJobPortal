using MediatR;
using SmartJobPortal.Application.Common;
using SmartJobPortal.Application.Interfaces;

namespace SmartJobPortal.Application.Features.Admin.Commands.ToggleJobStatus;

public class ToggleAdminJobStatusCommandHandler : IRequestHandler<ToggleAdminJobStatusCommand, ApiResponse<string>>
{
    private readonly IAdminRepository _adminRepo;
    private readonly ICacheService _cache;

    public ToggleAdminJobStatusCommandHandler(IAdminRepository adminRepo, ICacheService cache)
    {
        _adminRepo = adminRepo;
        _cache = cache;
    }

    public async Task<ApiResponse<string>> Handle(ToggleAdminJobStatusCommand request, CancellationToken cancellationToken)
    {
        var success = await _adminRepo.ToggleJobStatusAsync(request.JobId);
        if (!success)
            return ApiResponse<string>.NotFound("Job not found.");

        await _cache.RemoveAsync($"job:{request.JobId}");
        return ApiResponse<string>.Ok("Job status toggled successfully.");
    }
}
