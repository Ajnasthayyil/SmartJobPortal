using SmartJobPortal.Application.DTOs.Feed;
using SmartJobPortal.Application.Features.Feed.DTOs;

namespace SmartJobPortal.Application.Interfaces;

public interface IFeedHubService
{
    Task BroadcastNewPostAsync(FeedPostDto post);
    Task BroadcastNewCommentAsync(CommentDto comment, int postId);
    Task BroadcastReactionUpdateAsync(int postId, int newLikesCount);
}
