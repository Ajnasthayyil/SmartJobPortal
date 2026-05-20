using MediatR;
using SmartJobPortal.Application.Common;
using SmartJobPortal.Application.Interfaces;

namespace SmartJobPortal.Application.Features.Feed.Commands.DeleteComment;

public class DeleteCommentCommandHandler
    : IRequestHandler<DeleteCommentCommand, ApiResponse<bool>>
{
    private readonly IPostRepository _repository;

    public DeleteCommentCommandHandler(
        IPostRepository repository)
    {
        _repository = repository;
    }

    public async Task<ApiResponse<bool>> Handle(
        DeleteCommentCommand request,
        CancellationToken cancellationToken)
    {
        var comment = await _repository
            .GetCommentByIdAsync(request.CommentId);

        if (comment == null)
            return ApiResponse<bool>
                .Fail("Comment not found");

        // Allow deletion if the user is either the author of the comment
        // OR the author of the parent post (for moderation purposes)
        bool isAuthorized = comment.UserId == request.UserId;

        if (!isAuthorized)
        {
            var post = await _repository.GetPostByIdAsync(comment.PostId);
            if (post != null && post.UserId == request.UserId)
            {
                isAuthorized = true;
            }
        }

        if (!isAuthorized)
            return ApiResponse<bool>
                .Fail("Unauthorized");

        await _repository.DeleteCommentAsync(request.CommentId);

        return ApiResponse<bool>
            .Ok(true, "Comment deleted successfully");
    }
}
