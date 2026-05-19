using MediatR;
using SmartJobPortal.Application.Features.Feed.DTOs;

namespace SmartJobPortal.Application.Features.Feed.Queries.GetPostReactions;

public class GetPostReactionsQuery : IRequest<List<ReactionDto>>
{
    public int PostId { get; set; }
}
