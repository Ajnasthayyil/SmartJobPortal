using MediatR;
using SmartJobPortal.Application.Common;

namespace SmartJobPortal.Application.Features.Feed.Commands.DeletePost;

public class DeletePostCommand : IRequest<ApiResponse<bool>>
{
    public int PostId { get; set; }
    public int UserId { get; set; }
}
