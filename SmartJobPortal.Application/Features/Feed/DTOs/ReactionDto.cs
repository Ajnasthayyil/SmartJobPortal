namespace SmartJobPortal.Application.Features.Feed.DTOs;

public class ReactionDto
{
    public int UserId { get; set; }
    public string UserName { get; set; } = null!;
    public string? ProfilePictureUrl { get; set; }
    public string ReactionType { get; set; } = null!;
}
