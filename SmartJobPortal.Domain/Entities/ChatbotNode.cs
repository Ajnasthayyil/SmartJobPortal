using System;

namespace SmartJobPortal.Domain.Entities;

public class ChatbotNode
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public int? ParentId { get; set; }
    public string? RouteUrl { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}
