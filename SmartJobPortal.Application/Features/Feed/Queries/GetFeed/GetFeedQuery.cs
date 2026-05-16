using MediatR;
using SmartJobPortal.Application.Common;
using SmartJobPortal.Application.DTOs.Feed;

namespace SmartJobPortal.Application.Features.Feed.Queries.GetFeed;

public class GetFeedQuery
    : IRequest<ApiResponse<List<FeedPostDto>>>
{
    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 10;
}