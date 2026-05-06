using System.Text;
using System.Text.RegularExpressions;

namespace SmartJobPortal.Application.Services.ResumeLogic;

public static class ResumeTextSanitiser
{
    // Known prompt injection patterns
    private static readonly string[] InjectionPatterns =
    {
        @"ignore\s+(all\s+)?(previous|above|prior)\s+instructions?",
        @"forget\s+(all\s+)?(previous|above|prior)\s+instructions?",
        @"you\s+are\s+(now|a|an)",
        @"act\s+as\s+(a|an|if)",
        @"pretend\s+(you|to\s+be)",
        @"disregard\s+(all|previous|your)",
        @"new\s+instructions?",
        @"system\s*:\s*",
        @"<\s*system\s*>",
        @"\[INST\]",
        @"###\s*(instruction|system|prompt)",
        @"do\s+not\s+follow",
        @"override\s+(your|the|all)",
        @"jailbreak",
        @"dan\s+mode",
        @"developer\s+mode",
        @"sudo\s+mode"
    };

    public static string Sanitise(string rawText)
    {
        if (string.IsNullOrWhiteSpace(rawText))
            return string.Empty;

        // 1 — Normalise whitespace
        var text = Regex.Replace(rawText, @"\s+", " ");

        // 2 — Remove non-printable characters (hidden text tricks)
        text = Regex.Replace(text,
            @"[^\x20-\x7E\u00A0-\uFFFF\n\r\t]", " ");

        // 3 — Remove HTML/XML tags (embedded HTML injection)
        text = Regex.Replace(text, @"<[^>]+>", " ");

        // 4 — Check for prompt injection patterns
        var injectionFound = InjectionPatterns.Any(pattern =>
            Regex.IsMatch(text, pattern,
                RegexOptions.IgnoreCase | RegexOptions.Multiline));

        if (injectionFound)
        {
            // Log the attempt (in production, alert security team)
            Console.WriteLine(
                $"[SECURITY] Prompt injection attempt detected in resume. " +
                $"Returning empty text.");
            return string.Empty; // Reject the entire content
        }

        // 5 — Limit length (prevents token flooding)
        if (text.Length > 15_000)
            text = text[..15_000];

        return text.Trim();
    }

    // Checks if rejection should happen based on suspicious density
    public static bool IsSuspicious(string text)
    {
        // If text has very few letters but many special chars → suspicious
        var letterCount = text.Count(char.IsLetter);
        var specialCount = text.Count(c => !char.IsLetterOrDigit(c) && c != ' ');

        if (text.Length > 100 && letterCount < text.Length * 0.3)
            return true;

        // Suspicious keyword density
        var suspiciousWords = new[]
        {
            "ignore", "override", "forget", "pretend",
            "jailbreak", "disregard", "bypass"
        };

        var wordCount = suspiciousWords.Count(w =>
            text.Contains(w, StringComparison.OrdinalIgnoreCase));

        return wordCount >= 2;
    }
}