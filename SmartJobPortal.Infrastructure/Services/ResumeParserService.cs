using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;
using Mscc.GenerativeAI;
using Mscc.GenerativeAI.Types;
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

    public async Task<ResumeDto?> ParseResumeAsync(string resumeText)
    {
        try
        {
            var apiKey = _config["AI:ApiKey"];
            var googleAI = new GoogleAI(apiKey);

            // Use the most reliable model for 2026 Free Tier
            var model = googleAI.GenerativeModel("gemini-2.5-flash");

            // REMOVE config if it's causing 400, or simplify it
            // Some library versions fail if you set ResponseMimeType here.
            var config = new GenerationConfig { Temperature = 0.1f };

            // Ensure text isn't empty and isn't too long
            if (string.IsNullOrWhiteSpace(resumeText)) return null;
            string safeText = resumeText.Length > 8000 ? resumeText.Substring(0, 8000) : resumeText;

            // We move the JSON instruction into the prompt for maximum compatibility
            var prompt = $@"
Return a JSON object ONLY. Do not include markdown or explanations.
Fields: fullName, email, phone, skills (array), totalExperience (number), education (array), workExperience (array).

Resume:
{safeText}";

            // Call without the complex config if 400 persists
            var response = await model.GenerateContent(prompt, config);
            var raw = response.Text;

            if (string.IsNullOrWhiteSpace(raw)) return null;

            // Robust JSON Extraction
            var jsonMatch = Regex.Match(raw, @"\{[\s\S]*\}");
            if (!jsonMatch.Success) return null;

            return JsonConvert.DeserializeObject<ResumeDto>(jsonMatch.Value);
        }
        catch (Exception ex)
        {
            // Check this file! If it says "INVALID_ARGUMENT", your prompt is the problem.
            await File.WriteAllTextAsync("ai_error_400.txt", ex.ToString());
            return null;
        }
    }
}
