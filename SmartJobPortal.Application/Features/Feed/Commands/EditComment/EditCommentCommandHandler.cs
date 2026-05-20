using MediatR;
using SmartJobPortal.Application.Common;
using SmartJobPortal.Application.Interfaces;

namespace SmartJobPortal.Application.Features.Feed.Commands.EditComment;

public class EditCommentCommandHandler
    : IRequestHandler<EditCommentCommand, ApiResponse<bool>>
{
    private readonly IPostRepository _repository;

    public EditCommentCommandHandler(
        IPostRepository repository)
    {
        _repository = repository;
    }

    public async Task<ApiResponse<bool>> Handle(
        EditCommentCommand request,
        CancellationToken cancellationToken)
    {
        var comment = await _repository
            .GetCommentByIdAsync(request.CommentId);

        if (comment == null)
            return ApiResponse<bool>
                .Fail("Comment not found");

        if (comment.UserId != request.UserId)
            return ApiResponse<bool>
                .Fail("Unauthorized");

        comment.Content = request.Content;
        comment.UpdatedAt = DateTime.UtcNow;

        await _repository.UpdateCommentAsync(comment);

        return ApiResponse<bool>
            .Ok(true, "Comment updated");
    }
}