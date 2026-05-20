using MediatR;
using SmartJobPortal.Application.Common;
using SmartJobPortal.Application.Interfaces;

namespace SmartJobPortal.Application.Features.Feed.Commands.DeletePost;

public class DeletePostCommandHandler
    : IRequestHandler<DeletePostCommand, ApiResponse<bool>>
{
    private readonly IPostRepository _repository;

    public DeletePostCommandHandler(
        IPostRepository repository)
    {
        _repository = repository;
    }

    public async Task<ApiResponse<bool>> Handle(
        DeletePostCommand request,
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

        await _repository.DeletePostAsync(request.PostId);

        return ApiResponse<bool>
            .Ok(true, "Post deleted successfully");
    }
}
