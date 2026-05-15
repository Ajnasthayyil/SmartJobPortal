using Microsoft.AspNetCore.Http;

namespace SmartJobPortal.Application.Features.Resume.Common;

public class ResumeFileValidator
{
    private static readonly string[] AllowedExtensions = { ".pdf", ".docx" };
    private const long MaxFileSize = 5 * 1024 * 1024; // 5 MB

    public (bool IsValid, string? ErrorMessage) Validate(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return (false, "No file provided.");

        if (file.Length > MaxFileSize)
            return (false, "File size exceeds 5MB limit.");

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(ext))
            return (false, "Only PDF and DOCX files are allowed.");

        return (true, null);
    }
}
