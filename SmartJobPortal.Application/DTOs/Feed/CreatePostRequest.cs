namespace SmartJobPortal.Application.DTOs.Feed;

public class CreatePostRequest
{
    public string Content { get; set; } = string.Empty;

    public string? ImageUrl { get; set; }
}