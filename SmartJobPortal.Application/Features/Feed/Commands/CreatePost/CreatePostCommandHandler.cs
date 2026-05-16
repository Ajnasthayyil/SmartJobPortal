using MediatR;
using SmartJobPortal.Application.Common;
using SmartJobPortal.Application.Interfaces;
using SmartJobPortal.Domain.Entities;

namespace SmartJobPortal.Application.Features.Feed.Commands.CreatePost;

public class CreatePostCommandHandler
    : IRequestHandler<CreatePostCommand, ApiResponse<int>>
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
            ImageUrl = request.ImageUrl,
            CreatedAt = DateTime.UtcNow
        };

        var id = await _repo.CreateAsync(post);

        return ApiResponse<int>.SuccessResponse(
            id,
            "Post created successfully");
    }
}