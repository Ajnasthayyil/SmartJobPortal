using MediatR;
using SmartJobPortal.Application.Common;
using SmartJobPortal.Application.Interfaces;

namespace SmartJobPortal.Application.Features.Feed.Commands.EditPost;

public class EditPostCommandHandler
    : IRequestHandler<EditPostCommand, ApiResponse<bool>>
{
    private readonly IPostRepository _repository;

    public EditPostCommandHandler(
        IPostRepository repository)
    {
        _repository = repository;
    }

    public async Task<ApiResponse<bool>> Handle(
        EditPostCommand request,
        CancellationToken cancellationToken)
    {
        var post = await _repository
            .GetPostByIdAsync(request.PostId);

        if (post == null)
            return ApiResponse<bool>
                .Fail("Post not found");

        if (post.UserId != request.UserId)
            return ApiResponse<bool>
                .Fail("Unauthorized");

        post.Content = request.Content;
        post.UpdatedAt = DateTime.UtcNow;

        await _repository.UpdatePostAsync(post);

        return ApiResponse<bool>
            .Ok(true, "Post updated");
    }
}