using MediatR;
using SmartJobPortal.Application.Common;
using SmartJobPortal.Application.Interfaces;
using SmartJobPortal.Domain.Entities;

namespace SmartJobPortal.Application.Features.Feed.Commands.CreateComment;

public class CreateCommentCommandHandler
    : IRequestHandler<CreateCommentCommand,
        ApiResponse<int>>
{
    private readonly IPostRepository _repo;

    public CreateCommentCommandHandler(
        IPostRepository repo)
    {
        _repo = repo;
    }

    public async Task<ApiResponse<int>> Handle(
        CreateCommentCommand request,
        CancellationToken cancellationToken)
    {
        var comment = new PostComment
        {
            PostId = request.PostId,
            UserId = request.UserId,
            ParentCommentId = request.ParentCommentId,
            Content = request.Content,
            CreatedAt = DateTime.UtcNow
        };

        var id = await _repo.CreateCommentAsync(comment);

        return ApiResponse<int>.Ok(
            id,
            "Comment added successfully.");
    }
}