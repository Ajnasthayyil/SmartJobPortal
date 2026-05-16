namespace SmartJobPortal.Domain.Entities;

public class Post
{
    public int PostId { get; set; }

    public int UserId { get; set; }

    public string Content { get; set; } = string.Empty;

    public string? ImageUrl { get; set; }

    public int LikesCount { get; set; }

    public int CommentsCount { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}