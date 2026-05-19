using MediatR;
using SmartJobPortal.Application.Features.Feed.DTOs;
using SmartJobPortal.Application.Interfaces;

namespace SmartJobPortal.Application.Features.Feed.Queries.GetPostReactions;

public class GetPostReactionsQueryHandler : IRequestHandler<GetPostReactionsQuery, List<ReactionDto>>
{
    private readonly IPostRepository _repo;

    public GetPostReactionsQueryHandler(IPostRepository repo)
    {
        _repo = repo;
    }

    public async Task<List<ReactionDto>> Handle(GetPostReactionsQuery request, CancellationToken cancellationToken)
    {
        return await _repo.GetPostReactionsAsync(request.PostId);
    }
}
