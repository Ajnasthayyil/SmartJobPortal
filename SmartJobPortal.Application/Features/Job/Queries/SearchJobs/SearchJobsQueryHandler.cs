using MediatR;
using SmartJobPortal.Application.Common;
using SmartJobPortal.Application.DTOs.Candidate;
using SmartJobPortal.Application.Features.MatchScore.Queries.GetBulkMatchScores;
using SmartJobPortal.Application.Interfaces;

namespace SmartJobPortal.Application.Features.Job.Queries.SearchJobs;

public class SearchJobsQueryHandler : IRequestHandler<SearchJobsQuery, ApiResponse<JobSearchResponse>>
{
    private readonly IJobRepository _jobRepo;
    private readonly ICacheService _cache;
    private readonly IMediator _mediator;

    public SearchJobsQueryHandler(IJobRepository jobRepo, ICacheService cache, IMediator mediator)
    {
        _jobRepo = jobRepo;
        _cache = cache;
        _mediator = mediator;
    }

    public async Task<ApiResponse<JobSearchResponse>> Handle(SearchJobsQuery request, CancellationToken cancellationToken)
    {
        var cacheKey = $"jobsearch:{request.Request.ToCacheKey()}";
        var cached = await _cache.GetAsync<JobSearchResponse>(cacheKey);

        JobSearchResponse response;

        if (cached != null)
        {
            response = cached;
        }
        else
        {
            var (jobs, total) = await _jobRepo.SearchAsync(request.Request);
            response = new JobSearchResponse
            {
                TotalCount = total,
                Page = request.Request.Page,
                PageSize = request.Request.PageSize,
                Jobs = jobs
            };
            await _cache.SetAsync(cacheKey, response, TimeSpan.FromMinutes(2));
        }

        if (request.UserId > 0)
            await AttachScoresAsync(request.UserId, response.Jobs);

        return ApiResponse<JobSearchResponse>.Ok(response);
    }

    private async Task AttachScoresAsync(int userId, List<JobListItem> jobs)
    {
        var jobIds = jobs.Select(j => j.JobId).ToList();
        var results = await _mediator.Send(new GetBulkMatchScoresQuery(userId, jobIds));

        if (!results.Success || results.Data == null) return;

        var scoreMap = results.Data.ToDictionary(s => s.JobId);
        foreach (var job in jobs)
        {
            if (scoreMap.TryGetValue(job.JobId, out var s))
                job.MatchScore = s.TotalScore;
        }
    }
}
