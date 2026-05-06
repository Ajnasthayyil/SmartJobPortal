using System.Text.RegularExpressions;

namespace SmartJobPortal.Application.Services.ResumeLogic;

public class ResumeSkillExtractor
{
    private static readonly HashSet<string> KnownSkills = new(StringComparer.OrdinalIgnoreCase)
    {
        "React","Angular","Vue","Next.js","JavaScript","TypeScript","HTML","CSS","Sass","Tailwind",
        "Node.js","Express","NestJS","Django","Flask","FastAPI","Spring Boot","ASP.NET","C#",".NET","Java","Python","Go","Rust",
        "SQL","MySQL","PostgreSQL","MongoDB","Redis","Docker","Kubernetes","AWS","Azure","GCP","CI/CD","Git"
    };

    private static readonly List<string> KnownDegrees = new()
    {
        "Bachelor of Technology", "B.Tech", "BTech", "Bachelor of Engineering", "B.E.", "BE",
        "Master of Technology", "M.Tech", "MTech", "Master of Science", "M.Sc", "MSc",
        "Bachelor of Science", "B.Sc", "BSc", "MBA", "BBA", "PhD", "Doctorate", "Diploma",
        "MCA", "BCA", "B.Com", "M.Com"
    };

    private static readonly List<string> KnownMajors = new()
    {
        "Computer Science", "Information Technology", "Mechanical Engineering", "Electrical Engineering",
        "Civil Engineering", "Data Science", "Artificial Intelligence", "Business Administration"
    };

    private static readonly List<string> KnownRoles = new()
    {
        "Software Engineer", "Full Stack Developer", "Backend Developer", "Frontend Developer",
        "Mobile App Developer", "DevOps Engineer", "Data Scientist", "Data Analyst",
        "UI/UX Designer", "Product Manager", "Project Manager", "Quality Assurance",
        "QA Engineer", "System Administrator", "Network Engineer", "Technical Lead", "Intern"
    };

    public ResumeExtractionResult Extract(string sanitisedText)
    {
        var result = new ResumeExtractionResult();
        if (string.IsNullOrWhiteSpace(sanitisedText)) return result;

        var lines = sanitisedText.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

        // 1 — Global Skill Search (Still whitelist based)
        result.Skills = ExtractSkills(sanitisedText);

        // 2 — Line-by-Line Context Extraction
        foreach (var line in lines)
        {
            var trimmedLine = line.Trim();
            if (trimmedLine.Length < 3) continue;

            // Check for Education
            var degree = KnownDegrees.FirstOrDefault(d => trimmedLine.Contains(d, StringComparison.OrdinalIgnoreCase));
            if (degree != null)
            {
                var major = KnownMajors.FirstOrDefault(m => trimmedLine.Contains(m, StringComparison.OrdinalIgnoreCase));
                var entry = major != null ? $"{degree} in {major}" : degree;
                if (!result.EducationLines.Contains(entry)) result.EducationLines.Add(entry);
                continue;
            }

            // Check for Roles
            var role = KnownRoles.FirstOrDefault(r => trimmedLine.Contains(r, StringComparison.OrdinalIgnoreCase));
            if (role != null)
            {
                if (!result.ExperienceLines.Contains(role)) result.ExperienceLines.Add(role);
            }
        }

        result.ExperienceYears = ExtractExperienceYears(sanitisedText);
        result.Email = ExtractEmail(sanitisedText);
        result.WordCount = sanitisedText.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;

        return result;
    }

    private static List<string> ExtractSkills(string text)
    {
        var found = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var skill in KnownSkills)
        {
            if (Regex.IsMatch(text, $@"\b{Regex.Escape(skill)}\b", RegexOptions.IgnoreCase))
                found.Add(skill);
        }
        return found.OrderBy(s => s).ToList();
    }

    private static int ExtractExperienceYears(string text)
    {
        var match = Regex.Match(text, @"(\d+)\s*\+?\s*years?\s+of?\s*experience", RegexOptions.IgnoreCase);
        return match.Success && int.TryParse(match.Groups[1].Value, out var y) ? Math.Min(y, 50) : 0;
    }

    private static string? ExtractEmail(string text)
    {
        var match = Regex.Match(text, @"[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}");
        return match.Success ? match.Value : null;
    }
}

public class ResumeExtractionResult
{
    public List<string> Skills { get; set; } = new();
    public List<string> EducationLines { get; set; } = new();
    public List<string> ExperienceLines { get; set; } = new();
    public int ExperienceYears { get; set; }
    public string? Email { get; set; }
    public int WordCount { get; set; }
    public bool IsValidResume => WordCount >= 50 && (Skills.Count > 0 || ExperienceLines.Count > 0);
}