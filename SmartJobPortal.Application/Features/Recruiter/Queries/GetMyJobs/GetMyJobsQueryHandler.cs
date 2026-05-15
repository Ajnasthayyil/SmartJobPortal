using MediatR;
using SmartJobPortal.Application.Common;
using SmartJobPortal.Application.DTOs.Recruiter;
using SmartJobPortal.Application.Interfaces;

namespace SmartJobPortal.Application.Features.Recruiter.Queries.GetMyJobs;

public class GetMyJobsQueryHandler : IRequestHandler<GetMyJobsQuery, ApiResponse<List<JobResponse>>>
{
    private readonly IRecruiterRepository _recruiterRepo;
    private readonly IRecruiterJobRepository _jobRepo;

    public GetMyJobsQueryHandler(IRecruiterRepository recruiterRepo, IRecruiterJobRepository jobRepo)
    {
        _recruiterRepo = recruiterRepo;
        _jobRepo = jobRepo;
    }

    public async Task<ApiResponse<List<JobResponse>>> Handle(GetMyJobsQuery request, CancellationToken cancellationToken)
    {
        var recruiter = await _recruiterRepo.GetByUserIdAsync(request.UserId);
        if (recruiter == null) return ApiResponse<List<JobResponse>>.Fail("Access denied.", 403);

        var jobs = await _jobRepo.GetJobsByRecruiterIdAsync(recruiter.RecruiterId);
        return ApiResponse<List<JobResponse>>.Ok(jobs);
    }
}
