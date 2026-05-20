
using MediatR;
using SmartJobPortal.Application.Common;

namespace SmartJobPortal.Application.Features.Feed.Commands.EditPost;

public class EditPostCommand : IRequest<ApiResponse<bool>>
{
    public int PostId { get; set; }

    public int UserId { get; set; }

    public string Content { get; set; } = string.Empty;
}