namespace SmartJobPortal.Application.Features.Feed.DTOs;

public class CommentDto
{
    public int PostCommentId { get; set; }

    public int PostId { get; set; }

    public int UserId { get; set; }

    public string UserName { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;

    public int? ParentCommentId { get; set; }

    public DateTime CreatedAt { get; set; }

    public List<CommentDto> Replies { get; set; } = new();
}