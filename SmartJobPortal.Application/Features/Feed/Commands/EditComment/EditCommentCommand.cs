using MediatR;
using SmartJobPortal.Application.Common;

namespace SmartJobPortal.Application.Features.Feed.Commands.EditComment;

public class EditCommentCommand
    : IRequest<ApiResponse<bool>>
{
    public int CommentId { get; set; }

    public int UserId { get; set; }

    public string Content { get; set; } = string.Empty;
}