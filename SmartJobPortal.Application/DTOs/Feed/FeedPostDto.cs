using SmartJobPortal.Application.Features.Feed.DTOs;

namespace SmartJobPortal.Application.DTOs.Feed;

public class FeedPostDto
{
    public int PostId { get; set; }

    public int UserId { get; set; }

    public string UserName { get; set; } = string.Empty;

    public string? UserProfilePicture { get; set; }

    public string Content { get; set; } = string.Empty;

    public string? ImageUrl { get; set; }

    public List<string> Images { get; set; } = new();

    public int LikesCount { get; set; }

    public int CommentsCount { get; set; }

    public DateTime CreatedAt { get; set; }

    public List<PostReactionDto> Reactions { get; set; } = new();

    public string? CurrentUserReaction { get; set; }

    public int TotalReactions { get; set; }
}