using MediatR;
using SmartJobPortal.Application.Common;
using SmartJobPortal.Application.DTOs.Recruiter;
using SmartJobPortal.Application.Interfaces;

namespace SmartJobPortal.Application.Features.Recruiter.Commands.UpdateJob;

public class UpdateRecruiterJobCommandHandler : IRequestHandler<UpdateRecruiterJobCommand, ApiResponse<string>>
{
    private readonly IRecruiterRepository _recruiterRepo;
    private readonly IRecruiterJobRepository _jobRepo;
    private readonly ICacheService _cache;

    public UpdateRecruiterJobCommandHandler(IRecruiterRepository recruiterRepo, IRecruiterJobRepository jobRepo, ICacheService cache)
    {
        _recruiterRepo = recruiterRepo;
        _jobRepo = jobRepo;
        _cache = cache;
    }

    public async Task<ApiResponse<string>> Handle(UpdateRecruiterJobCommand command, CancellationToken cancellationToken)
    {
        var recruiter = await _recruiterRepo.GetByUserIdAsync(command.UserId);
        if (recruiter == null) return ApiResponse<string>.Fail("Access denied.", 403);

        var job = await _jobRepo.GetJobByIdAsync(command.JobId);
        if (job == null || job.RecruiterId != recruiter.RecruiterId)
            return ApiResponse<string>.NotFound("Job not found or access denied.");

        // Update properties
        job.Title = command.Request.Title;
        job.Description = command.Request.Description;
        job.Location = command.Request.Location;
        job.JobType = command.Request.JobType;
        job.MinSalary = command.Request.MinSalary;
        job.MaxSalary = command.Request.MaxSalary;
        job.MinExperienceYears = command.Request.MinExperienceYears;
        job.ExpiresAt = command.Request.ExpiresAt;
        job.UpdatedAt = DateTime.Now;

        var updated = await _jobRepo.UpdateJobAsync(job);
        if (!updated) return ApiResponse<string>.Fail("Failed to update job.");

        await _cache.RemoveAsync($"job:{command.JobId}");
        return ApiResponse<string>.Ok("Job updated successfully.");
    }
}
