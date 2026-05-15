using System.Text.RegularExpressions;

namespace SmartJobPortal.Application.Features.Resume.Common;

public static class ResumeTextSanitiser
{
    public static string Sanitise(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;

        // Remove suspicious prompt injection patterns
        var patterns = new[]
        {
            @"ignore\s+all\s+previous\s+instructions",
            @"you\s+are\s+now\s+an\s+admin",
            @"system\s+override",
            @"<script",
            @"javascript:"
        };

        foreach (var p in patterns)
        {
            text = Regex.Replace(text, p, "[REDACTED]", RegexOptions.IgnoreCase);
        }

        return text.Trim();
    }

    public static bool IsSuspicious(string text)
    {
        // Prevent extremely short or empty documents from being processed
        if (text.Length < 20) return true; 
        return false;
    }
}
