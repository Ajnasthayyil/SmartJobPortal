using MediatR;
using SmartJobPortal.Application.Common;
using SmartJobPortal.Application.DTOs.Recruiter;
using SmartJobPortal.Application.Interfaces;

namespace SmartJobPortal.Application.Features.Recruiter.Queries.GetJobDetail;

public class GetRecruiterJobDetailQueryHandler : IRequestHandler<GetRecruiterJobDetailQuery, ApiResponse<JobResponse>>
{
    private readonly IRecruiterRepository _recruiterRepo;
    private readonly IRecruiterJobRepository _jobRepo;

    public GetRecruiterJobDetailQueryHandler(IRecruiterRepository recruiterRepo, IRecruiterJobRepository jobRepo)
    {
        _recruiterRepo = recruiterRepo;
        _jobRepo = jobRepo;
    }

    public async Task<ApiResponse<JobResponse>> Handle(GetRecruiterJobDetailQuery request, CancellationToken cancellationToken)
    {
        var recruiter = await _recruiterRepo.GetByUserIdAsync(request.UserId);
        if (recruiter == null) return ApiResponse<JobResponse>.Fail("Access denied.", 403);

        var jobs = await _jobRepo.GetJobsByRecruiterIdAsync(recruiter.RecruiterId);
        var job = jobs.FirstOrDefault(j => j.JobId == request.JobId);
        
        if (job == null)
            return ApiResponse<JobResponse>.NotFound("Job not found.");

        return ApiResponse<JobResponse>.Ok(job);
    }
}
