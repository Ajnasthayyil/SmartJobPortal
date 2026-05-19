using MediatR;
using SmartJobPortal.Application.Common;
using SmartJobPortal.Application.Interfaces;

namespace SmartJobPortal.Application.Features.Feed.Commands.ReactPost;

public class ReactPostCommandHandler
    : IRequestHandler<ReactPostCommand,
        ApiResponse<bool>>
{
    private readonly IPostRepository _repo;

    public ReactPostCommandHandler(
        IPostRepository repo)
    {
        _repo = repo;
    }

    public async Task<ApiResponse<bool>> Handle(
        ReactPostCommand request,
        CancellationToken cancellationToken)
    {
        await _repo.ReactToPostAsync(
            request.PostId,
            request.UserId,
            request.ReactionType);

        return ApiResponse<bool>.Ok(
            true,
            "Reaction updated");
    }
}