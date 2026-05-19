using MediatR;
using SmartJobPortal.Application.Common;
using SmartJobPortal.Application.DTOs.Feed;
using SmartJobPortal.Application.Interfaces;

namespace SmartJobPortal.Application.Features.Feed.Queries.GetFeed;

public class GetFeedQueryHandler
    : IRequestHandler<GetFeedQuery,
        ApiResponse<List<FeedPostDto>>>
{
    private readonly IPostRepository _repo;

    public GetFeedQueryHandler(
        IPostRepository repo)
    {
        _repo = repo;
    }

    public async Task<ApiResponse<List<FeedPostDto>>> Handle(
        GetFeedQuery request,
        CancellationToken cancellationToken)
    {
        var posts = await _repo.GetFeedAsync(
            request.Page,
            request.PageSize,
            request.CurrentUserId);

        return ApiResponse<List<FeedPostDto>>
            .SuccessResponse(posts);
    }
}