using MediatR;
using SmartJobPortal.Application.Common;
using SmartJobPortal.Application.DTOs.Candidate;
using SmartJobPortal.Application.Features.MatchScore.Queries.GetMatchScore;
using SmartJobPortal.Application.Interfaces;

namespace SmartJobPortal.Application.Features.Job.Queries.GetMyApplications;

public class GetMyApplicationsQueryHandler : IRequestHandler<GetMyApplicationsQuery, ApiResponse<List<ApplicationTrackingResponse>>>
{
    private readonly ICandidateRepository _candidateRepo;
    private readonly IApplicationRepository _appRepo;
    private readonly IMediator _mediator;

    public GetMyApplicationsQueryHandler(
        ICandidateRepository candidateRepo,
        IApplicationRepository appRepo,
        IMediator mediator)
    {
        _candidateRepo = candidateRepo;
        _appRepo = appRepo;
        _mediator = mediator;
    }

    public async Task<ApiResponse<List<ApplicationTrackingResponse>>> Handle(GetMyApplicationsQuery request, CancellationToken cancellationToken)
    {
        var candidate = await _candidateRepo.GetByUserIdAsync(request.UserId);
        if (candidate == null)
            return ApiResponse<List<ApplicationTrackingResponse>>.Ok(new());

        var applications = await _appRepo.GetByCandidateIdAsync(candidate.CandidateId);

        var allStatuses = new[]
        {
            "Applied", "UnderReview", "Shortlisted", "Interview", "Offered"
        };

        foreach (var app in applications)
        {
            var currentIndex = Array.IndexOf(allStatuses, app.Status);
            app.Timeline = allStatuses.Select((status, i) => new StatusTimelineItem
            {
                Status = status,
                IsCompleted = i < currentIndex,
                IsCurrent = i == currentIndex,
                OccurredAt = i <= currentIndex ? app.AppliedAt.AddDays(i) : null
            }).ToList();

            var scoreResult = await _mediator.Send(new GetMatchScoreQuery(request.UserId, app.JobId));
            if (scoreResult.Success)
                app.MatchScore = scoreResult.Data?.TotalScore;
        }

        return ApiResponse<List<ApplicationTrackingResponse>>.Ok(applications);
    }
}
