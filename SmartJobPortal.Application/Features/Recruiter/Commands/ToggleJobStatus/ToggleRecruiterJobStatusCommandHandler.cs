using MediatR;
using SmartJobPortal.Application.Common;
using SmartJobPortal.Application.Interfaces;

namespace SmartJobPortal.Application.Features.Recruiter.Commands.ToggleJobStatus;

public class ToggleRecruiterJobStatusCommandHandler : IRequestHandler<ToggleRecruiterJobStatusCommand, ApiResponse<string>>
{
    private readonly IRecruiterRepository _recruiterRepo;
    private readonly IRecruiterJobRepository _jobRepo;
    private readonly ICacheService _cache;

    public ToggleRecruiterJobStatusCommandHandler(IRecruiterRepository recruiterRepo, IRecruiterJobRepository jobRepo, ICacheService cache)
    {
        _recruiterRepo = recruiterRepo;
        _jobRepo = jobRepo;
        _cache = cache;
    }

    public async Task<ApiResponse<string>> Handle(ToggleRecruiterJobStatusCommand command, CancellationToken cancellationToken)
    {
        var recruiter = await _recruiterRepo.GetByUserIdAsync(command.UserId);
        if (recruiter == null) return ApiResponse<string>.Fail("Access denied.", 403);

        var toggled = await _jobRepo.ToggleJobStatusAsync(command.JobId, recruiter.RecruiterId);
        if (!toggled) return ApiResponse<string>.NotFound("Job not found or access denied.");

        await _cache.RemoveAsync($"job:{command.JobId}");
        return ApiResponse<string>.Ok("Job status toggled.");
    }
}
