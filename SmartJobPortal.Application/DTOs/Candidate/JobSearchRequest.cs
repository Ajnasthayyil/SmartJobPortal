namespace SmartJobPortal.Application.DTOs.Candidate;

public class JobSearchRequest
{
    public string? Keyword { get; set; }
    public string? Location { get; set; }
    public string? JobType { get; set; }
    public int? MinSalary { get; set; }
    public int? MaxSalary { get; set; }
    public int? MinExperience { get; set; }
    public int? MaxExperience { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;

    public string ToCacheKey()
    {
        var raw = $"{Keyword}|{Location}|{JobType}|{MinSalary}|{MaxSalary}" +
                  $"|{MinExperience}|{MaxExperience}|{Page}|{PageSize}";
        var bytes = System.Text.Encoding.UTF8.GetBytes(raw);
        var hash = System.Security.Cryptography.SHA256.HashData(bytes);
        return Convert.ToHexString(hash)[..16];
    }
}