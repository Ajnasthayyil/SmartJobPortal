using MediatR;
using SmartJobPortal.Application.Common;
using SmartJobPortal.Application.DTOs.Candidate;
using SmartJobPortal.Application.Features.MatchScore.Queries.GetMatchScore;
using SmartJobPortal.Application.Interfaces;
using SmartJobPortal.Domain.Entities;

namespace SmartJobPortal.Application.Features.Job.Queries.GetJobDetail;

public class GetJobDetailQueryHandler : IRequestHandler<GetJobDetailQuery, ApiResponse<JobDetail>>
{
    private readonly IJobRepository _jobRepo;
    private readonly ICacheService _cache;
    private readonly IMediator _mediator;

    public GetJobDetailQueryHandler(IJobRepository jobRepo, ICacheService cache, IMediator mediator)
    {
        _jobRepo = jobRepo;
        _cache = cache;
        _mediator = mediator;
    }

    public async Task<ApiResponse<JobDetail>> Handle(GetJobDetailQuery request, CancellationToken cancellationToken)
    {
        var cacheKey = $"job:{request.JobId}";
        var job = await _cache.GetAsync<JobDetail>(cacheKey)
                  ?? await _jobRepo.GetDetailAsync(request.JobId);

        if (job == null)
            return ApiResponse<JobDetail>.NotFound("Job not found.");

        await _cache.SetAsync(cacheKey, job, TimeSpan.FromMinutes(30));

        if (request.UserId > 0)
        {
            var scoreResult = await _mediator.Send(new GetMatchScoreQuery(request.UserId, request.JobId));
            if (scoreResult.Success)
                job.MatchScore = scoreResult.Data;
        }

        return ApiResponse<JobDetail>.Ok(job);
    }
}
