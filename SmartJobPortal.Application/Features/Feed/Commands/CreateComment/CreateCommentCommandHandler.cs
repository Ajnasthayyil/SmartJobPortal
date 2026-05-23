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
    private readonly IFeedHubService _feedHub;

    public CreateCommentCommandHandler(
        IPostRepository repo,
        IFeedHubService feedHub)
    {
        _repo = repo;
        _feedHub = feedHub;
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

        var commentDto = await _repo.GetCommentDtoByIdAsync(id);
        if (commentDto != null)
        {
            await _feedHub.BroadcastNewCommentAsync(commentDto, request.PostId);
        }

        return ApiResponse<int>.Ok(
            id,
            "Comment added successfully.");
    }
}