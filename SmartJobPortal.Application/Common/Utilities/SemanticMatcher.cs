using System.Text.RegularExpressions;

namespace SmartJobPortal.Application.Common.Utilities;

public class SemanticMatcher : ISemanticMatcher
{
    private static readonly Dictionary<string, List<string>> _synonyms = new(StringComparer.OrdinalIgnoreCase)
    {
        { "JS", new() { "JavaScript", "ECMAScript" } },
        { "JavaScript", new() { "JS", "ECMAScript" } },
        { "C#", new() { ".NET", "ASP.NET", "DotNet" } },
        { ".NET", new() { "C#", "DotNet" } },
        { "Teamwork", new() { "Team Collaboration", "Collaboration", "Team Player" } },
        { "Team Collaboration", new() { "Teamwork", "Collaboration" } },
        { "Problem Solving", new() { "Analytical Thinking", "Critical Thinking", "Troubleshooting" } },
        { "Analytical Thinking", new() { "Problem Solving", "Critical Thinking" } },
        { "ASP.NET Core", new() { ".NET Core", "DotNet Core" } },
        { ".NET Core", new() { "ASP.NET Core" } },
        { "SQL", new() { "Database", "T-SQL", "MySQL", "PostgreSQL" } },
        { "Communication", new() { "Interpersonal Skills", "Verbal Communication", "Written Communication" } }
    };

    private static readonly string[] _noiseWords = { 
        "excellent", "good", "advanced", "basic", "strong", "proficient", "expert", "pro", 
        "senior", "junior", "intermediate", "knowledge of", "experience with", "skills in" 
    };

    public bool IsMatch(string candidateSkill, string jobSkill, out string reason)
    {
        if (string.IsNullOrWhiteSpace(candidateSkill) || string.IsNullOrWhiteSpace(jobSkill))
        {
            reason = "Empty input";
            return false;
        }

        // 1. Exact Match
        if (string.Equals(candidateSkill.Trim(), jobSkill.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            reason = "Exact match found.";
            return true;
        }

        // 2. Normalize (Remove punctuation, noise words, extra spaces)
        var cNorm = Normalize(candidateSkill);
        var jNorm = Normalize(jobSkill);

        if (cNorm == jNorm)
        {
            reason = $"Semantic match: Both represent '{jNorm}' after removing qualifiers like 'excellent' or 'good'.";
            return true;
        }

        // 3. Synonym Match
        if (_synonyms.TryGetValue(cNorm, out var cSyns) && cSyns.Any(s => string.Equals(Normalize(s), jNorm, StringComparison.OrdinalIgnoreCase)))
        {
            reason = $"Synonym match: '{candidateSkill}' is recognized as a professional synonym for '{jobSkill}'.";
            return true;
        }
        
        if (_synonyms.TryGetValue(jNorm, out var jSyns) && jSyns.Any(s => string.Equals(Normalize(s), cNorm, StringComparison.OrdinalIgnoreCase)))
        {
            reason = $"Synonym match: '{jobSkill}' is recognized as a professional synonym for '{candidateSkill}'.";
            return true;
        }

        // 4. Partial Match (e.g. "React.js" contains "React")
        if (cNorm.Contains(jNorm) || jNorm.Contains(cNorm))
        {
            reason = $"Partial match: One term contains the other ('{cNorm}' vs '{jNorm}').";
            return true;
        }

        reason = "No semantic match found.";
        return false;
    }

    private string Normalize(string input)
    {
        var result = input.ToLowerInvariant();
        
        // Remove punctuation
        result = Regex.Replace(result, @"[^\w\s#+.]", "");

        // Remove noise words
        foreach (var word in _noiseWords)
        {
            // Match word with boundaries to avoid "pro" matching "programming"
            result = Regex.Replace(result, $@"\b{Regex.Escape(word)}\b", "");
        }

        // Clean up spaces
        result = Regex.Replace(result, @"\s+", " ").Trim();
        
        return result;
    }
}
