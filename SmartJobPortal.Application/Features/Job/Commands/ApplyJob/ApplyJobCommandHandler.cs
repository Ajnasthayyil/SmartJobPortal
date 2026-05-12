using MediatR;
using SmartJobPortal.Application.Common;
using SmartJobPortal.Application.Features.MatchScore.Queries.GetMatchScore;
using SmartJobPortal.Application.Interfaces;
using SmartJobPortal.Domain.Entities;

namespace SmartJobPortal.Application.Features.Job.Commands.ApplyJob;

public class ApplyJobCommandHandler : IRequestHandler<ApplyJobCommand, ApiResponse<int>>
{
    private readonly IJobRepository _jobRepo;
    private readonly ICandidateRepository _candidateRepo;
    private readonly IApplicationRepository _appRepo;
    private readonly IMediator _mediator;

    public ApplyJobCommandHandler(
        IJobRepository jobRepo,
        ICandidateRepository candidateRepo,
        IApplicationRepository appRepo,
        IMediator mediator)
    {
        _jobRepo = jobRepo;
        _candidateRepo = candidateRepo;
        _appRepo = appRepo;
        _mediator = mediator;
    }

    public async Task<ApiResponse<int>> Handle(ApplyJobCommand command, CancellationToken cancellationToken)
    {
        var request = command.Request;
        var candidate = await _candidateRepo.GetByUserIdAsync(command.UserId);
        if (candidate == null)
            return ApiResponse<int>.Fail("Complete your profile before applying.");

        if (!candidate.HasResume())
            return ApiResponse<int>.Fail("Upload a resume before applying.");

        if (await _appRepo.AlreadyAppliedAsync(candidate.CandidateId, request.JobId))
            return ApiResponse<int>.Fail("You have already applied to this job.");

        var job = await _jobRepo.GetDetailAsync(request.JobId);
        if (job == null)
            return ApiResponse<int>.NotFound("Job not found.");

        var applicationId = await _appRepo.CreateAsync(new SmartJobPortal.Domain.Entities.Application
        {
            CandidateId = candidate.CandidateId,
            JobId = request.JobId,
            CoverNote = request.CoverNote,
            Status = "Applied",
            AppliedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        });

        // Pre-calculate score
        await _mediator.Send(new GetMatchScoreQuery(command.UserId, request.JobId));

        return ApiResponse<int>.Ok(applicationId, "Application submitted successfully.");
    }
}
