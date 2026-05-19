using MediatR;
using SmartJobPortal.Application.Common;

namespace SmartJobPortal.Application.Features.Feed.Commands.ReactPost;

public class ReactPostCommand
    : IRequest<ApiResponse<bool>>
{
    public int UserId { get; set; }

    public int PostId { get; set; }

    public string ReactionType { get; set; } = string.Empty;
}