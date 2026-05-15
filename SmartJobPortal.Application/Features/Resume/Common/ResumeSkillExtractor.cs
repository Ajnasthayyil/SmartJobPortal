using System.Text.RegularExpressions;

namespace SmartJobPortal.Application.Features.Resume.Common;

public class ResumeSkillExtractor
{
    public ResumeExtractionResult Extract(string text)
    {
        var result = new ResumeExtractionResult();
        
        // Email extraction
        var emailMatch = Regex.Match(text, @"[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}");
        if (emailMatch.Success) result.Email = emailMatch.Value;

        // Simple skill extraction (keywords)
        var commonSkills = new[] { "C#", "ASP.NET", "SQL", "Javascript", "React", "Python", "Java", "Docker", "AWS", "Azure" };
        foreach (var skill in commonSkills)
        {
            if (text.Contains(skill, StringComparison.OrdinalIgnoreCase))
                result.Skills.Add(skill);
        }

        // Experience detection (e.g., "5 years", "3+ years")
        var expMatch = Regex.Match(text, @"(\d+)\+?\s*years?");
        if (expMatch.Success && int.TryParse(expMatch.Groups[1].Value, out var years))
            result.ExperienceYears = years;

        result.IsValidResume = result.Skills.Any() || !string.IsNullOrEmpty(result.Email);

        return result;
    }
}

public class ResumeExtractionResult
{
    public string? Email { get; set; }
    public List<string> Skills { get; set; } = new();
    public int ExperienceYears { get; set; }
    public bool IsValidResume { get; set; }
}
