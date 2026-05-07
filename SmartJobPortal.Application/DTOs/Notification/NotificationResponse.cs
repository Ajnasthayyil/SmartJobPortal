namespace SmartJobPortal.Application.DTOs.NotificationDTOs;

public class NotificationResponse
{
    public int NotificationId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? JobTitle { get; set; }
    public string? CompanyName { get; set; }
    public string TimeAgo => GetTimeAgo(CreatedAt);

    private static string GetTimeAgo(DateTime dt)
    {
        var diff = DateTime.Now - dt;
        if (diff.TotalMinutes < 1) return "just now";
        if (diff.TotalMinutes < 60) return $"{(int)diff.TotalMinutes}m ago";
        if (diff.TotalHours < 24) return $"{(int)diff.TotalHours}h ago";
        if (diff.TotalDays < 7) return $"{(int)diff.TotalDays}d ago";
        return dt.ToString("MMM d");
    }
}

public class UnreadCountResponse
{
    public int Count { get; set; }
}