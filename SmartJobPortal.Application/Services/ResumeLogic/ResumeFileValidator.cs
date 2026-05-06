using Microsoft.AspNetCore.Http;

namespace SmartJobPortal.Application.Services.ResumeLogic;

public class ResumeFileValidator
{
    // Magic bytes — the REAL file type check (not just extension)
    private static readonly byte[] PdfMagic = { 0x25, 0x50, 0x44, 0x46 }; // %PDF
    private static readonly byte[] DocxMagic = { 0x50, 0x4B, 0x03, 0x04 }; // PK (ZIP)

    private const int MaxFileSizeBytes = 5 * 1024 * 1024; // 5 MB

    public (bool isValid, string error) Validate(IFormFile file)
    {
        // 1 — Size check
        if (file.Length > MaxFileSizeBytes)
            return (false, "File size must be under 5MB.");

        if (file.Length < 1024) // Less than 1KB — not a real resume
            return (false, "File is too small to be a valid resume.");

        // 2 — Magic bytes check (not just extension!)
        using var stream = file.OpenReadStream();
        var header = new byte[4];
        stream.Read(header, 0, 4);
        stream.Position = 0;

        var isPdf = header.Take(4).SequenceEqual(PdfMagic);
        var isDocx = header.Take(4).SequenceEqual(DocxMagic);

        if (!isPdf && !isDocx)
            return (false, "Only PDF and DOCX files are accepted.");

        // 3 — Extension must match magic bytes
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (isPdf && ext != ".pdf")
            return (false, "File extension does not match file content.");
        if (isDocx && ext != ".docx" && ext != ".doc")
            return (false, "File extension does not match file content.");

        return (true, string.Empty);
    }
}