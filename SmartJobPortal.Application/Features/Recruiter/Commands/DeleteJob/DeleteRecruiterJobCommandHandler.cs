using MediatR;
using SmartJobPortal.Application.Common;
using SmartJobPortal.Application.Interfaces;

namespace SmartJobPortal.Application.Features.Recruiter.Commands.DeleteJob;

public class DeleteRecruiterJobCommandHandler : IRequestHandler<DeleteRecruiterJobCommand, ApiResponse<string>>
{
    private readonly IRecruiterRepository _recruiterRepo;
    private readonly IRecruiterJobRepository _jobRepo;
    private readonly ICacheService _cache;

    public DeleteRecruiterJobCommandHandler(IRecruiterRepository recruiterRepo, IRecruiterJobRepository jobRepo, ICacheService cache)
    {
        _recruiterRepo = recruiterRepo;
        _jobRepo = jobRepo;
        _cache = cache;
    }

    public async Task<ApiResponse<string>> Handle(DeleteRecruiterJobCommand command, CancellationToken cancellationToken)
    {
        var recruiter = await _recruiterRepo.GetByUserIdAsync(command.UserId);
        if (recruiter == null) return ApiResponse<string>.Fail("Access denied.", 403);

        var deleted = await _jobRepo.SoftDeleteJobAsync(command.JobId, recruiter.RecruiterId);
        if (!deleted) return ApiResponse<string>.NotFound("Job not found or access denied.");

        await _cache.RemoveAsync($"job:{command.JobId}");
        return ApiResponse<string>.Ok("Job deleted successfully.");
    }
}
