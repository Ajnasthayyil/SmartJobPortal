using Microsoft.AspNetCore.SignalR;
using SmartJobPortal.API.Hubs;
using SmartJobPortal.Application.DTOs.Feed;
using SmartJobPortal.Application.Features.Feed.DTOs;
using SmartJobPortal.Application.Interfaces;

namespace SmartJobPortal.API.Services;

public class FeedHubService : IFeedHubService
{
    private readonly IHubContext<FeedHub> _hubContext;

    public FeedHubService(IHubContext<FeedHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public async Task BroadcastNewPostAsync(FeedPostDto post)
    {
        await _hubContext.Clients.All.SendAsync("ReceiveNewPost", post);
    }

    public async Task BroadcastNewCommentAsync(CommentDto comment, int postId)
    {
        await _hubContext.Clients.All.SendAsync("ReceiveNewComment", comment, postId);
    }

    public async Task BroadcastReactionUpdateAsync(int postId, int newLikesCount)
    {
        await _hubContext.Clients.All.SendAsync("ReceiveReactionUpdate", postId, newLikesCount);
    }
}
