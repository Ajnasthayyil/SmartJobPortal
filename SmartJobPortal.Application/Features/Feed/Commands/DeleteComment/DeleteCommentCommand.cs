using MediatR;
using SmartJobPortal.Application.Common;

namespace SmartJobPortal.Application.Features.Feed.Commands.DeleteComment;

public class DeleteCommentCommand : IRequest<ApiResponse<bool>>
{
    public int CommentId { get; set; }
    public int UserId { get; set; }
}
