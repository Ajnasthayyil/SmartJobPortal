using MediatR;
using SmartJobPortal.Application.Common;
using SmartJobPortal.Application.DTOs.Feed;

namespace SmartJobPortal.Application.Features.Feed.Commands.CreatePost;

public class CreatePostCommand
    : IRequest<ApiResponse<int>>
{
    public int UserId { get; set; }

    public string Content { get; set; } = string.Empty;

    public List<UploadedMediaDto> Images { get; set; } = new();
}