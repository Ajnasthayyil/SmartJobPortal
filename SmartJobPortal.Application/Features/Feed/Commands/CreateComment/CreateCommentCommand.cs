using MediatR;
using SmartJobPortal.Application.Common;

namespace SmartJobPortal.Application.Features.Feed.Commands.CreateComment;

public class CreateCommentCommand
    : IRequest<ApiResponse<int>>
{
    public int PostId { get; set; }

    public int UserId { get; set; }

    public int? ParentCommentId { get; set; }

    public string Content { get; set; } = string.Empty;
}