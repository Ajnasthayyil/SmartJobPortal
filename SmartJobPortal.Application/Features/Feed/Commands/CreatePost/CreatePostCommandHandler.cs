using MediatR;
using SmartJobPortal.Application.Common;
using SmartJobPortal.Application.Interfaces;
using SmartJobPortal.Domain.Entities;

namespace SmartJobPortal.Application.Features.Feed.Commands.CreatePost;

public class CreatePostCommandHandler
    : IRequestHandler<CreatePostCommand,
        ApiResponse<int>>
{
    private readonly IPostRepository _repo;
    private readonly IFeedHubService _feedHub;

    public CreatePostCommandHandler(
        IPostRepository repo,
        IFeedHubService feedHub)
    {
        _repo = repo;
        _feedHub = feedHub;
    }

    public async Task<ApiResponse<int>> Handle(
        CreatePostCommand request,
        CancellationToken cancellationToken)
    {
        var post = new Post
        {
            UserId = request.UserId,
            Content = request.Content,
            ImageUrl = request.Images.FirstOrDefault()?.Url,
            CreatedAt = DateTime.UtcNow
        };

        var postId = await _repo.CreateAsync(post);

        if (request.Images.Any())
        {
            var media = request.Images
                .Select((x, i) => new PostMedia
                {
                    PostId = postId,
                    MediaUrl = x.Url,
                    PublicId = x.PublicId,
                    DisplayOrder = i
                })
                .ToList();

            await _repo.AddMediaAsync(media);
        }

        // Fetch full DTO for broadcast
        var postDto = await _repo.GetFeedPostByIdAsync(postId, request.UserId);
        if (postDto != null)
        {
            await _feedHub.BroadcastNewPostAsync(postDto);
        }

        return ApiResponse<int>.SuccessResponse(
            postId,
            "Post created");
    }
}