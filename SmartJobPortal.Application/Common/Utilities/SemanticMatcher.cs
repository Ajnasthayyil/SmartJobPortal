using System.Text.RegularExpressions;

namespace SmartJobPortal.Application.Common.Utilities;

/// <summary>
/// Advanced semantic skill matcher with:
/// - Multi-directional synonym graph (tech aliases, acronyms, abbreviations)
/// - Token-based Jaccard similarity for compound skill phrases
/// - Framework → language partial credit (e.g. React implies JavaScript)
/// - Version-agnostic matching (.NET 6 == .NET 8 == .NET)
/// - Noise-word stripping for qualifier-polluted skill strings
/// </summary>
public class SemanticMatcher : ISemanticMatcher
{
    // ── Synonym Graph ─────────────────────────────────────────────────────────
    // Each entry maps a canonical term to all its known aliases/synonyms.
    // Lookups are bidirectional: both sides are checked.
    private static readonly Dictionary<string, HashSet<string>> _synonyms =
        new(StringComparer.OrdinalIgnoreCase)
    {
        // ── JavaScript Ecosystem ───────────────────────────────────────────
        ["JavaScript"]      = new(OIC) { "JS", "ECMAScript", "ES6", "ES2015", "ES2020", "Vanilla JS", "Node.js", "NodeJS", "Node" },
        ["TypeScript"]      = new(OIC) { "TS" },
        ["React"]           = new(OIC) { "React.js", "ReactJS", "React JS" },
        ["Angular"]         = new(OIC) { "Angular.js", "AngularJS", "Angular 2+", "Angular 14", "Angular 16", "Angular 17" },
        ["Vue"]             = new(OIC) { "Vue.js", "VueJS", "Vue 3" },
        ["Next.js"]         = new(OIC) { "NextJS", "Next JS" },
        ["Nuxt.js"]         = new(OIC) { "NuxtJS", "Nuxt" },
        ["Node.js"]         = new(OIC) { "NodeJS", "Node", "Express", "Express.js", "JavaScript" },
        ["Express.js"]      = new(OIC) { "Express", "ExpressJS", "Node.js" },

        // ── .NET / C# Ecosystem ────────────────────────────────────────────
        ["C#"]              = new(OIC) { ".NET", "DotNet", "ASP.NET", "ASP.NET Core", ".NET Core", "Dot Net" },
        [".NET"]            = new(OIC) { "C#", "DotNet", "ASP.NET", "ASP.NET Core", ".NET Core", "Dot Net" },
        ["ASP.NET Core"]    = new(OIC) { "ASP.NET", ".NET Core", "C#", "DotNet Core" },
        [".NET Core"]       = new(OIC) { "ASP.NET Core", "C#", ".NET" },
        ["Entity Framework"] = new(OIC) { "EF", "EF Core", "Entity Framework Core" },

        // ── Python Ecosystem ───────────────────────────────────────────────
        ["Python"]          = new(OIC) { "Python 3", "Python 3.x", "CPython", "Py" },
        ["Django"]          = new(OIC) { "Django REST Framework", "DRF", "Python" },
        ["Flask"]           = new(OIC) { "Flask API", "Python" },
        ["FastAPI"]         = new(OIC) { "Fast API", "Python" },
        ["NumPy"]           = new(OIC) { "Numpy", "Numeric Python" },
        ["Pandas"]          = new(OIC) { "Pandas DataFrame", "Data Analysis" },

        // ── Java Ecosystem ─────────────────────────────────────────────────
        ["Java"]            = new(OIC) { "Java SE", "Java EE", "J2EE", "JVM" },
        ["Spring"]          = new(OIC) { "Spring Boot", "Spring Framework", "Spring MVC", "Java" },
        ["Spring Boot"]     = new(OIC) { "Spring", "Java", "Spring Framework" },
        ["Hibernate"]       = new(OIC) { "JPA", "Java Persistence API" },
        ["Maven"]           = new(OIC) { "Gradle", "Build Tool" },

        // ── PHP / Ruby / Go / Rust ─────────────────────────────────────────
        ["PHP"]             = new(OIC) { "Laravel", "Symfony", "WordPress", "PHP 8" },
        ["Laravel"]         = new(OIC) { "PHP", "Lumen" },
        ["Ruby"]            = new(OIC) { "Ruby on Rails", "RoR", "Rails" },
        ["Go"]              = new(OIC) { "Golang", "Go Language" },
        ["Rust"]            = new(OIC) { "Rust Lang", "Systems Programming" },

        // ── Mobile Development ─────────────────────────────────────────────
        ["Swift"]           = new(OIC) { "iOS Development", "Xcode", "iOS" },
        ["Kotlin"]          = new(OIC) { "Android Development", "Android", "Jetpack Compose" },
        ["React Native"]    = new(OIC) { "React-Native", "Cross-Platform Mobile" },
        ["Flutter"]         = new(OIC) { "Dart", "Cross-Platform Mobile" },
        ["Android"]         = new(OIC) { "Kotlin", "Android SDK", "Android Studio", "Java" },
        ["iOS"]             = new(OIC) { "Swift", "Objective-C", "Xcode", "UIKit", "SwiftUI" },

        // ── Databases – SQL ────────────────────────────────────────────────
        ["SQL"]             = new(OIC) { "T-SQL", "TSQL", "MySQL", "PostgreSQL", "MSSQL", "MS SQL", "SQLite", "Database", "RDBMS", "Relational Database" },
        ["MySQL"]           = new(OIC) { "SQL", "MariaDB", "Database" },
        ["PostgreSQL"]      = new(OIC) { "Postgres", "SQL", "Database" },
        ["Microsoft SQL Server"] = new(OIC) { "MSSQL", "MS SQL", "SQL Server", "T-SQL" },
        ["SQLite"]          = new(OIC) { "SQL", "Embedded Database" },
        ["Oracle"]          = new(OIC) { "Oracle DB", "PL/SQL", "Oracle Database" },

        // ── Databases – NoSQL ──────────────────────────────────────────────
        ["MongoDB"]         = new(OIC) { "Mongo", "NoSQL", "Document Database" },
        ["Redis"]           = new(OIC) { "Cache", "In-Memory Database", "NoSQL" },
        ["Cassandra"]       = new(OIC) { "Apache Cassandra", "NoSQL", "Wide Column Store" },
        ["Elasticsearch"]   = new(OIC) { "Elastic Search", "ELK Stack", "Lucene" },
        ["Firebase"]        = new(OIC) { "Firestore", "Google Firebase", "Realtime Database" },

        // ── Cloud Platforms ────────────────────────────────────────────────
        ["AWS"]             = new(OIC) { "Amazon Web Services", "Amazon AWS", "Cloud" },
        ["Azure"]           = new(OIC) { "Microsoft Azure", "Azure Cloud", "Cloud" },
        ["GCP"]             = new(OIC) { "Google Cloud", "Google Cloud Platform", "Cloud" },
        ["Cloud"]           = new(OIC) { "AWS", "Azure", "GCP", "Cloud Computing", "Cloud Infrastructure" },

        // ── DevOps / Infrastructure ────────────────────────────────────────
        ["Docker"]          = new(OIC) { "Containerization", "Container", "Docker Compose" },
        ["Kubernetes"]      = new(OIC) { "K8s", "Container Orchestration", "Helm" },
        ["CI/CD"]           = new(OIC) { "Continuous Integration", "Continuous Deployment", "GitHub Actions", "Jenkins", "GitLab CI", "Azure DevOps", "DevOps" },
        ["Jenkins"]         = new(OIC) { "CI/CD", "Continuous Integration" },
        ["Terraform"]       = new(OIC) { "Infrastructure as Code", "IaC" },
        ["Ansible"]         = new(OIC) { "Configuration Management", "IaC" },
        ["Linux"]           = new(OIC) { "Unix", "Ubuntu", "CentOS", "Debian", "Shell Scripting", "Bash" },
        ["Bash"]            = new(OIC) { "Shell Scripting", "Linux", "Shell Script" },
        ["Git"]             = new(OIC) { "GitHub", "GitLab", "Bitbucket", "Version Control", "Source Control" },
        ["GitHub"]          = new(OIC) { "Git", "Version Control", "GitLab" },

        // ── AI / ML / Data Science ─────────────────────────────────────────
        ["Machine Learning"] = new(OIC) { "ML", "AI", "Deep Learning", "Artificial Intelligence" },
        ["Deep Learning"]    = new(OIC) { "Neural Networks", "ML", "AI", "TensorFlow", "PyTorch" },
        ["TensorFlow"]       = new(OIC) { "TF", "Machine Learning", "Deep Learning" },
        ["PyTorch"]          = new(OIC) { "Torch", "Deep Learning", "Machine Learning" },
        ["Data Science"]     = new(OIC) { "Data Analysis", "Analytics", "Machine Learning", "Statistics" },
        ["Power BI"]         = new(OIC) { "PowerBI", "Business Intelligence", "BI", "Data Visualization" },
        ["Tableau"]          = new(OIC) { "Data Visualization", "BI", "Business Intelligence" },

        // ── APIs & Protocols ───────────────────────────────────────────────
        ["REST API"]         = new(OIC) { "RESTful API", "REST", "RESTful", "RESTful Web Services", "HTTP API", "Web API" },
        ["GraphQL"]          = new(OIC) { "Graph QL" },
        ["gRPC"]             = new(OIC) { "Protocol Buffers", "Protobuf" },
        ["WebSocket"]        = new(OIC) { "WebSockets", "Real-time Communication" },
        ["Microservices"]    = new(OIC) { "Micro Services", "Service-Oriented Architecture", "SOA", "Distributed Systems" },
        ["Message Queue"]    = new(OIC) { "RabbitMQ", "Kafka", "Message Broker", "Event-Driven Architecture" },
        ["RabbitMQ"]         = new(OIC) { "Message Queue", "AMQP", "Message Broker" },
        ["Apache Kafka"]     = new(OIC) { "Kafka", "Message Queue", "Event Streaming" },

        // ── Testing ────────────────────────────────────────────────────────
        ["Unit Testing"]     = new(OIC) { "TDD", "Test Driven Development", "xUnit", "NUnit", "JUnit", "Jest", "Mocha" },
        ["Selenium"]         = new(OIC) { "Test Automation", "UI Testing", "Browser Automation" },
        ["Jest"]             = new(OIC) { "JavaScript Testing", "Unit Testing" },
        ["Cypress"]          = new(OIC) { "E2E Testing", "End-to-End Testing", "UI Testing" },

        // ── Architecture / Design Patterns ─────────────────────────────────
        ["OOP"]              = new(OIC) { "Object Oriented Programming", "Object-Oriented", "Object Oriented Design" },
        ["SOLID"]            = new(OIC) { "SOLID Principles", "Design Patterns", "Clean Code" },
        ["Design Patterns"]  = new(OIC) { "SOLID", "GoF Patterns", "Software Architecture" },
        ["MVC"]              = new(OIC) { "Model View Controller", "MVVM", "Model View ViewModel" },

        // ── Security ───────────────────────────────────────────────────────
        ["OAuth"]            = new(OIC) { "OAuth2", "OpenID Connect", "SSO", "Authentication" },
        ["JWT"]              = new(OIC) { "JSON Web Token", "Authentication", "Token Based Auth" },
        ["Cybersecurity"]    = new(OIC) { "Information Security", "InfoSec", "Penetration Testing", "Security" },

        // ── Frontend / UI ──────────────────────────────────────────────────
        ["HTML"]             = new(OIC) { "HTML5", "Markup Language", "Web Development" },
        ["CSS"]              = new(OIC) { "CSS3", "Stylesheet", "SCSS", "SASS", "Tailwind CSS", "Bootstrap" },
        ["SCSS"]             = new(OIC) { "SASS", "CSS", "CSS Preprocessor" },
        ["Bootstrap"]        = new(OIC) { "CSS Framework", "CSS", "Responsive Design" },
        ["Tailwind CSS"]     = new(OIC) { "Tailwind", "CSS", "CSS Framework" },
        ["Figma"]            = new(OIC) { "UI Design", "UX Design", "Prototype", "Adobe XD" },
        ["UI/UX"]            = new(OIC) { "User Interface", "User Experience", "UX Design", "UI Design" },

        // ── Soft Skills / General ──────────────────────────────────────────
        ["Communication"]    = new(OIC) { "Interpersonal Skills", "Verbal Communication", "Written Communication", "Presentation Skills" },
        ["Teamwork"]         = new(OIC) { "Team Collaboration", "Collaboration", "Team Player", "Cross-functional Teams" },
        ["Problem Solving"]  = new(OIC) { "Analytical Thinking", "Critical Thinking", "Troubleshooting", "Debugging" },
        ["Leadership"]       = new(OIC) { "Team Leadership", "People Management", "Mentoring" },
        ["Agile"]            = new(OIC) { "Scrum", "Kanban", "Sprint", "Agile Methodology", "SAFe" },
        ["Scrum"]            = new(OIC) { "Agile", "Sprint", "Kanban", "Scrum Master" },
        ["Project Management"] = new(OIC) { "PMP", "Stakeholder Management", "Delivery Management" },
        ["Microsoft Office"] = new(OIC) { "MS Office", "Word", "Excel", "PowerPoint", "Office 365" },
        ["Excel"]            = new(OIC) { "Microsoft Excel", "Spreadsheets", "VBA", "Data Analysis" },
    };

    // ── Noise Words (qualifiers stripped before comparison) ───────────────────
    private static readonly string[] _noiseWords =
    {
        "excellent", "good", "advanced", "basic", "strong", "proficient",
        "expert", "pro", "senior", "junior", "intermediate", "knowledge of",
        "experience with", "skills in", "familiarity with", "understanding of",
        "working knowledge", "hands-on", "proven", "solid", "extensive"
    };

    private static StringComparer OIC => StringComparer.OrdinalIgnoreCase;

    // ── Public API ────────────────────────────────────────────────────────────

    public bool IsMatch(string candidateSkill, string jobSkill, out string reason)
    {
        if (string.IsNullOrWhiteSpace(candidateSkill) || string.IsNullOrWhiteSpace(jobSkill))
        {
            reason = "Empty input.";
            return false;
        }

        // 1. Exact match (fast path)
        if (string.Equals(candidateSkill.Trim(), jobSkill.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            reason = "Exact match.";
            return true;
        }

        var cNorm = Normalize(candidateSkill);
        var jNorm = Normalize(jobSkill);

        // 2. Normalized exact match (strips qualifiers like "proficient in")
        if (cNorm == jNorm)
        {
            reason = $"Normalized match: both resolve to '{jNorm}' after stripping qualifiers.";
            return true;
        }

        // 3. Version-agnostic match  e.g. "Python 3.10" → "Python"
        if (VersionAgnosticMatch(cNorm, jNorm, out reason)) return true;

        // 4. Full synonym graph lookup (bidirectional)
        if (SynonymMatch(cNorm, jNorm, out reason)) return true;

        // 5. Partial / substring containment  e.g. "React.js" ↔ "React"
        if (ContainsMatch(cNorm, jNorm, out reason)) return true;

        // 6. Token Jaccard similarity for compound phrases  
        //    e.g. "RESTful API Design" ↔ "REST API"
        if (JaccardTokenMatch(cNorm, jNorm, out reason)) return true;

        reason = "No semantic match found.";
        return false;
    }

    // ── Matching Layers ───────────────────────────────────────────────────────

    /// Strip trailing version numbers: "python 3.10" → "python", ".net 8" → ".net"
    private static bool VersionAgnosticMatch(string cNorm, string jNorm, out string reason)
    {
        var cBase = Regex.Replace(cNorm, @"\s*[\d]+(?:[\.\d]+)*\s*$", "").Trim();
        var jBase = Regex.Replace(jNorm, @"\s*[\d]+(?:[\.\d]+)*\s*$", "").Trim();

        if (!string.IsNullOrEmpty(cBase) && !string.IsNullOrEmpty(jBase) && cBase == jBase)
        {
            reason = $"Version-agnostic match: '{cNorm}' and '{jNorm}' are different versions of the same technology.";
            return true;
        }
        reason = string.Empty;
        return false;
    }

    private static bool SynonymMatch(string cNorm, string jNorm, out string reason)
    {
        // Look up cNorm in synonym graph and check if jNorm is in its alias set
        foreach (var (canonical, aliases) in _synonyms)
        {
            bool cIsCanonical = string.Equals(cNorm, canonical, OrdCI_SC);
            bool cIsAlias     = aliases.Contains(cNorm);
            bool jIsCanonical = string.Equals(jNorm, canonical, OrdCI_SC);
            bool jIsAlias     = aliases.Contains(jNorm);

            if ((cIsCanonical || cIsAlias) && (jIsCanonical || jIsAlias))
            {
                reason = $"Synonym match via '{canonical}': '{cNorm}' and '{jNorm}' are recognized aliases.";
                return true;
            }
        }
        reason = string.Empty;
        return false;
    }

    private static bool ContainsMatch(string cNorm, string jNorm, out string reason)
    {
        // Protect against false positives from very short tokens (e.g. "c" inside "c#")
        if (jNorm.Length >= 3 && cNorm.Contains(jNorm))
        {
            reason = $"Partial match: candidate skill '{cNorm}' contains required skill '{jNorm}'.";
            return true;
        }
        if (cNorm.Length >= 3 && jNorm.Contains(cNorm))
        {
            reason = $"Partial match: required skill '{jNorm}' contains candidate skill '{cNorm}'.";
            return true;
        }
        reason = string.Empty;
        return false;
    }

    /// Jaccard similarity on word-tokens: score ≥ 0.5 is considered a match.
    /// e.g. {"restful","api","design"} ∩ {"rest","api"} / {"restful","api","design","rest"} = 0.5
    private static bool JaccardTokenMatch(string cNorm, string jNorm, out string reason)
    {
        var cTokens = Tokenize(cNorm);
        var jTokens = Tokenize(jNorm);

        // Skip single-token phrases — already handled by Contains
        if (cTokens.Count <= 1 || jTokens.Count <= 1)
        {
            reason = string.Empty;
            return false;
        }

        var intersection = cTokens.Intersect(jTokens, OIC).Count();
        var union        = cTokens.Union(jTokens, OIC).Count();
        if (union == 0)
        {
            reason = string.Empty;
            return false;
        }

        var jaccard = (double)intersection / union;
        if (jaccard >= 0.5)
        {
            reason = $"Token similarity match (Jaccard={jaccard:F2}): '{cNorm}' and '{jNorm}' share enough key terms.";
            return true;
        }
        reason = string.Empty;
        return false;
    }

    // ── Normalization ─────────────────────────────────────────────────────────

    private string Normalize(string input)
    {
        var result = input.ToLowerInvariant();

        // Remove noise qualifier phrases first (multi-word)
        foreach (var word in _noiseWords.OrderByDescending(w => w.Length))
            result = Regex.Replace(result, $@"\b{Regex.Escape(word)}\b", " ");

        // Keep word characters, spaces, and relevant punctuation: # + . /
        result = Regex.Replace(result, @"[^\w\s#+./]", " ");

        // Collapse whitespace
        result = Regex.Replace(result, @"\s+", " ").Trim();

        return result;
    }

    private static HashSet<string> Tokenize(string normalized)
    {
        return normalized
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .ToHashSet(OIC);
    }

    private static StringComparer OrdCI => StringComparer.OrdinalIgnoreCase;
    private static StringComparison OrdCI_SC => StringComparison.OrdinalIgnoreCase;
}
