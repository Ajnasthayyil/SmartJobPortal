namespace SmartJobPortal.Domain.Entities;

public class PostMedia
{
    public int PostMediaId { get; set; }

    public int PostId { get; set; }

    public string MediaUrl { get; set; } = string.Empty;

    public string PublicId { get; set; } = string.Empty;

    public string MediaType { get; set; } = "Image";

    public int DisplayOrder { get; set; }
}