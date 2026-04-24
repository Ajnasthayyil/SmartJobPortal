using System.Text.RegularExpressions;
using DocumentFormat.OpenXml.Packaging;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using Microsoft.Extensions.Configuration;
using Mscc.GenerativeAI;
using Newtonsoft.Json;
using SmartJobPortal.Application.DTOs.Candidate;
using SmartJobPortal.Application.Interfaces;

namespace SmartJobPortal.Infrastructure.Services;

public class ResumeParserService : IResumeParserService
{
    private readonly IConfiguration _config;

    public ResumeParserService(IConfiguration config)
    {
        _config = config;
    }

    public async Task<ResumeDto?> ParseResumeAsync(string filePath, string contentType)
    {
        try
        {
            // 1. Extract Text
            string resumeText = contentType switch
            {
                "application/pdf" => ExtractTextFromPdf(filePath),
                "application/vnd.openxmlformats-officedocument.wordprocessingml.document" => ExtractTextFromDocx(filePath),
                _ => throw new Exception("Unsupported file format for parsing")
            };

            if (string.IsNullOrWhiteSpace(resumeText)) return null;

            // 2. Call AI (Gemini)
            return await ParseWithGeminiAsync(resumeText);
        }
        catch (Exception ex)
        {
            // In a real app, log this
            Console.WriteLine($"Parsing Error: {ex.Message}");
            return null;
        }
    }

    private string ExtractTextFromPdf(string path)
    {
        using (var reader = new PdfReader(path))
        using (var pdfDoc = new PdfDocument(reader))
        {
            var text = new System.Text.StringBuilder();
            for (int i = 1; i <= pdfDoc.GetNumberOfPages(); i++)
            {
                text.Append(PdfTextExtractor.GetTextFromPage(pdfDoc.GetPage(i)));
            }
            return text.ToString();
        }
    }

    private string ExtractTextFromDocx(string path)
    {
        using (var wordDoc = WordprocessingDocument.Open(path, false))
        {
            var body = wordDoc.MainDocumentPart?.Document.Body;
            return body?.InnerText ?? string.Empty;
        }
    }

    private async Task<ResumeDto?> ParseWithGeminiAsync(string resumeText)
    {
        var apiKey = _config["AI:ApiKey"];
        if (string.IsNullOrEmpty(apiKey) || apiKey == "YOUR_API_KEY_HERE")
            throw new Exception("Gemini API Key is not configured.");

        var googleAI = new GoogleAI(apiKey);
        var model = googleAI.GenerativeModel("gemini-1.5-flash");

        var prompt = $@"
You are an AI Resume Parser.

Extract structured information from the resume text provided below.

Return ONLY valid JSON. Do NOT include explanations.

Required fields:
- fullName (string)
- email (string)
- phone (string)
- skills (array of strings)
- totalExperience (number in years, approximate if needed)
- education (array of objects with degree, institution, year)
- workExperience (array of objects with company, role, duration, description)

Rules:
- Extract accurate and relevant data only
- If data is missing, return null or empty array
- Skills should be normalized (e.g., 'ASP.NET Core', 'Angular', 'SQL Server')
- Remove duplicates in skills
- Experience should be calculated based on work history

Resume Text:
""""""
{resumeText}
""""""
";

        var response = await model.GenerateContent(prompt);
        var jsonResponse = response.Text;

        if (string.IsNullOrEmpty(jsonResponse)) return null;

        // Clean JSON response (sometimes AI adds markdown blocks)
        jsonResponse = Regex.Replace(jsonResponse, "```json", "", RegexOptions.IgnoreCase);
        jsonResponse = Regex.Replace(jsonResponse, "```", "", RegexOptions.IgnoreCase).Trim();

        return JsonConvert.DeserializeObject<ResumeDto>(jsonResponse);
    }
}
