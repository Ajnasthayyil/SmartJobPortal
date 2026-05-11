using System.Text;
using System.Text.RegularExpressions;

namespace SmartJobPortal.Application.Services.ResumeLogic;

public static class ResumeTextSanitiser
{
    // Known prompt injection patterns
    private static readonly string[] InjectionPatterns =
    {
        @"\bignore\s+(all\s+)?(previous|above|prior)\s+instructions?\b",
        @"\bforget\s+(all\s+)?(previous|above|prior)\s+instructions?\b",
        @"\bdisregard\s+(all|previous|your)\b",
        @"\bnew\s+instructions?\b",
        @"\bsystem\s+(prompt|message|instruction|role)\b", // Refined from \bsystem\s*:\s*
        @"<\s*system\s*>",
        @"\[INST\]",
        @"###\s*(instruction|system|prompt)",
        @"\bdo\s+not\s+follow\b",
        @"\boverride\s+(your|the|all)\b",
        @"\bjailbreak\b",
        @"\bdan\s+mode\b",
        @"\bdeveloper\s+mode\b",
        @"\bsudo\s+mode\b"
    };

    public static string Sanitise(string rawText)
    {
        if (string.IsNullOrWhiteSpace(rawText))
            return string.Empty;

        // Normalise whitespace (preserve newlines for structure)
        var text = Regex.Replace(rawText, @"[ \t]+", " ");
        text = Regex.Replace(text, @"(\r\n|\n){2,}", "\n\n"); // Collapse multiple newlines to max 2
        text = text.Trim();

        //  Remove non-printable characters (hidden text tricks)
        text = Regex.Replace(text,
            @"[^\x20-\x7E\u00A0-\uFFFF\n\r\t]", " ");

        // Remove HTML/XML tags (embedded HTML injection)
        text = Regex.Replace(text, @"<[^>]+>", " ");

        //  Remove prompt injection patterns
        foreach (var pattern in InjectionPatterns)
        {
            if (Regex.IsMatch(text, pattern, RegexOptions.IgnoreCase))
            {
                Console.WriteLine($"[SECURITY] Prompt-like pattern removed: {pattern}");
                text = Regex.Replace(text, pattern, "[REMOVED]", RegexOptions.IgnoreCase);
            }
        }

        // Limit length (prevents token flooding)
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

        return wordCount >= 3;
    }
}