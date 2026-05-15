using MediatR;
using SmartJobPortal.Application.Common;
using SmartJobPortal.Application.DTOs.Recruiter;
using SmartJobPortal.Application.Interfaces;

namespace SmartJobPortal.Application.Features.Recruiter.Queries.GetRankedApplicants;

public class GetRankedApplicantsQueryHandler : IRequestHandler<GetRankedApplicantsQuery, ApiResponse<List<ApplicantResponse>>>
{
    private readonly IRecruiterRepository _recruiterRepo;
    private readonly IRecruiterJobRepository _jobRepo;

    public GetRankedApplicantsQueryHandler(IRecruiterRepository recruiterRepo, IRecruiterJobRepository jobRepo)
    {
        _recruiterRepo = recruiterRepo;
        _jobRepo = jobRepo;
    }

    public async Task<ApiResponse<List<ApplicantResponse>>> Handle(GetRankedApplicantsQuery request, CancellationToken cancellationToken)
    {
        var recruiter = await _recruiterRepo.GetByUserIdAsync(request.UserId);
        if (recruiter == null) return ApiResponse<List<ApplicantResponse>>.Fail("Access denied.", 403);

        var applicants = await _jobRepo.GetApplicantsAsync(request.JobId);
        // Ranking is handled by sorting by MatchScore descending
        var ranked = applicants.OrderByDescending(a => a.TotalScore ?? 0).ToList();
        
        return ApiResponse<List<ApplicantResponse>>.Ok(ranked);
    }
}
