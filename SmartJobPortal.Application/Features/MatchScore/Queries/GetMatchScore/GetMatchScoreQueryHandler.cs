using MediatR;
using SmartJobPortal.Application.Common;
using SmartJobPortal.Application.DTOs.Candidate;
using SmartJobPortal.Application.Features.MatchScore.Common;
using SmartJobPortal.Application.Interfaces;

namespace SmartJobPortal.Application.Features.MatchScore.Queries.GetMatchScore;

public class GetMatchScoreQueryHandler : IRequestHandler<GetMatchScoreQuery, ApiResponse<MatchScoreResponse>>
{
    private readonly ICandidateRepository _candidateRepo;
    private readonly IMatchScoreRepository _matchScoreRepo;
    private readonly ICacheService _cache;
    private readonly MatchScoreHelper _helper;

    public GetMatchScoreQueryHandler(
        ICandidateRepository candidateRepo,
        IMatchScoreRepository matchScoreRepo,
        ICacheService cache,
        IJobRepository jobRepo,
        SmartJobPortal.Application.Common.Utilities.ISemanticMatcher matcher)
    {
        _candidateRepo = candidateRepo;
        _matchScoreRepo = matchScoreRepo;
        _cache = cache;
        _helper = new MatchScoreHelper(candidateRepo, jobRepo, matcher);
    }

    public async Task<ApiResponse<MatchScoreResponse>> Handle(GetMatchScoreQuery request, CancellationToken cancellationToken)
    {
        var candidate = await _candidateRepo.GetByUserIdAsync(request.UserId);
        if (candidate == null)
            return ApiResponse<MatchScoreResponse>.Fail("Complete your profile to see match scores.");

        var cacheKey = $"match:{candidate.CandidateId}:{request.JobId}";
        var cached = await _cache.GetAsync<MatchScoreResponse>(cacheKey);
        if (cached != null)
            return ApiResponse<MatchScoreResponse>.Ok(cached);

        var score = await _helper.CalculateAsync(candidate, request.JobId);
        if (score == null)
            return ApiResponse<MatchScoreResponse>.NotFound("Job not found.");

        await _matchScoreRepo.UpsertAsync(score);

        var response = await _helper.BuildResponseAsync(score, candidate.CandidateId, request.JobId);
        await _cache.SetAsync(cacheKey, response, TimeSpan.FromHours(1));

        return ApiResponse<MatchScoreResponse>.Ok(response);
    }
}
