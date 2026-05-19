using MediatR;
using SmartJobPortal.Application.Features.Feed.DTOs;
using SmartJobPortal.Application.Interfaces;

namespace SmartJobPortal.Application.Features.Feed.Queries.GetComments;

public class GetCommentsQueryHandler
    : IRequestHandler<GetCommentsQuery,
        List<CommentDto>>
{
    private readonly IPostRepository _repo;

    public GetCommentsQueryHandler(
        IPostRepository repo)
    {
        _repo = repo;
    }

    public async Task<List<CommentDto>> Handle(
        GetCommentsQuery request,
        CancellationToken cancellationToken)
    {
        var comments =
            await _repo.GetCommentsAsync(
                request.PostId);

        var rootComments = comments
            .Where(x => x.ParentCommentId == null)
            .ToList();

        foreach (var root in rootComments)
        {
            root.Replies = comments
                .Where(x =>
                    x.ParentCommentId ==
                    root.PostCommentId)
                .ToList();
        }

        return rootComments;
    }
}