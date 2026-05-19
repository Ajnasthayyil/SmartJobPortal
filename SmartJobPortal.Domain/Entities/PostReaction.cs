namespace SmartJobPortal.Domain.Entities;

public class PostReaction
{
    public int PostReactionId { get; set; }

    public int PostId { get; set; }

    public int UserId { get; set; }

    public string ReactionType { get; set; } = "Like";

    public DateTime CreatedAt { get; set; }
}