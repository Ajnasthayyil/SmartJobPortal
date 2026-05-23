using MediatR;
using SmartJobPortal.Application.Common;
using SmartJobPortal.Application.Interfaces;

namespace SmartJobPortal.Application.Features.Feed.Commands.ReactPost;

public class ReactPostCommandHandler
    : IRequestHandler<ReactPostCommand,
        ApiResponse<bool>>
{
    private readonly IPostRepository _repo;
    private readonly IFeedHubService _feedHub;

    public ReactPostCommandHandler(
        IPostRepository repo,
        IFeedHubService feedHub)
    {
        _repo = repo;
        _feedHub = feedHub;
    }

    public async Task<ApiResponse<bool>> Handle(
        ReactPostCommand request,
        CancellationToken cancellationToken)
    {
        await _repo.ReactToPostAsync(
            request.PostId,
            request.UserId,
            request.ReactionType);

        var post = await _repo.GetPostByIdAsync(request.PostId);
        if (post != null)
        {
            await _feedHub.BroadcastReactionUpdateAsync(request.PostId, post.LikesCount);
        }

        return ApiResponse<bool>.Ok(
            true,
            "Reaction updated");
    }
}