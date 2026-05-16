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

    public CreatePostCommandHandler(
        IPostRepository repo)
    {
        _repo = repo;
    }

    public async Task<ApiResponse<int>> Handle(
        CreatePostCommand request,
        CancellationToken cancellationToken)
    {
        var post = new Post
        {
            UserId = request.UserId,
            Content = request.Content,
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

        return ApiResponse<int>.SuccessResponse(
            postId,
            "Post created");
    }
}