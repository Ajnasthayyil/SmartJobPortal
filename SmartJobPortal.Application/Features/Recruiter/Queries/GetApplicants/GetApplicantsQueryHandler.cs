using MediatR;
using SmartJobPortal.Application.Common;
using SmartJobPortal.Application.DTOs.Recruiter;
using SmartJobPortal.Application.Interfaces;

namespace SmartJobPortal.Application.Features.Recruiter.Queries.GetApplicants;

public class GetApplicantsQueryHandler : IRequestHandler<GetApplicantsQuery, ApiResponse<List<ApplicantResponse>>>
{
    private readonly IRecruiterRepository _recruiterRepo;
    private readonly IRecruiterJobRepository _jobRepo;

    public GetApplicantsQueryHandler(IRecruiterRepository recruiterRepo, IRecruiterJobRepository jobRepo)
    {
        _recruiterRepo = recruiterRepo;
        _jobRepo = jobRepo;
    }

    public async Task<ApiResponse<List<ApplicantResponse>>> Handle(GetApplicantsQuery request, CancellationToken cancellationToken)
    {
        var recruiter = await _recruiterRepo.GetByUserIdAsync(request.UserId);
        if (recruiter == null) return ApiResponse<List<ApplicantResponse>>.Fail("Access denied.", 403);

        var applicants = await _jobRepo.GetApplicantsAsync(request.JobId);
        return ApiResponse<List<ApplicantResponse>>.Ok(applicants);
    }
}
