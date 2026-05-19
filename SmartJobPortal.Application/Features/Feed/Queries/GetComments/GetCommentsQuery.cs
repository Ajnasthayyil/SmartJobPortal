using MediatR;
using SmartJobPortal.Application.Features.Feed.DTOs;

namespace SmartJobPortal.Application.Features.Feed.Queries.GetComments;

public class GetCommentsQuery
    : IRequest<List<CommentDto>>
{
    public int PostId { get; set; }
}