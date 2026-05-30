using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using SmartJobPortal.Application.DTOs.Resume;
using SmartJobPortal.Application.Interfaces;
using SmartJobPortal.Application.DTOs.Candidate;

namespace SmartJobPortal.Infrastructure.Services
{
    public class AffindaParsingService : IAffindaParsingService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<AffindaParsingService> _logger;

        public AffindaParsingService(
            HttpClient httpClient, 
            IConfiguration configuration, 
            ILogger<AffindaParsingService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<AffindaResponseDto?> ParseAsync(string filePath, CancellationToken ct = default)
        {
            try
            {
                if (!File.Exists(filePath))
                    throw new FileNotFoundException($"File not found: {filePath}");

                var ext = Path.GetExtension(filePath).ToLowerInvariant();
                _logger.LogInformation("Processing resume: {FilePath} (extension: {Ext})", filePath, ext);

                if (ext == ".pdf")
                {
                    _logger.LogInformation("PDF format detected. Using Gemini Multimodal Cloud OCR Pipeline.");
                    return await ParsePdfDirectlyAsync(filePath, ct);
                }
                else
                {
                    _logger.LogInformation("Non-PDF format detected. Extracting text locally using OpenXML.");
                    var extractor = new NativeTextExtractor();
                    var rawText = await extractor.ExtractTextAsync(filePath);
                    var cleanText = CleanExtractedText(rawText);
                    
                    if (string.IsNullOrWhiteSpace(cleanText))
                    {
                        _logger.LogWarning("Extracted text is empty for file: {FilePath}", filePath);
                        cleanText = "[Empty resume text file]";
                    }
                    
                    return await ParseTextWithGeminiAsync(cleanText, ct);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception in upgraded AI resume parsing service.");
                return null;
            }
        }

        private string CleanExtractedText(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return string.Empty;
            var cleaned = Regex.Replace(text, @"[\u0000-\u0008\u000B-\u000C\u000E-\u001F]", "");
            cleaned = Regex.Replace(cleaned, @"\s+", " ");
            return cleaned.Trim();
        }

        private string GetGeminiUrl()
        {
            var apiKey = _configuration["AI:ApiKey"]
                         ?? _configuration["Gemini:ApiKey"] 
                         ?? _configuration["HuggingFace:ApiKey"] 
                         ?? "AIzaSyCDofbZ7T5vmA_dJniHNBjmPVOdSNKi75U";
            return $"https://generativelanguage.googleapis.com/v1beta/models/gemini-1.5-flash:generateContent?key={apiKey}";
        }

        private string GetSystemPrompt()
        {
            return @"
You are a highly accurate, state-of-the-art AI Resume Parser.
Analyze the provided document and extract all details into a strictly valid JSON object matching the following structure.
Do NOT output any markdown formatting, backticks, or explanation. Return ONLY the JSON object.

JSON Schema:
{
  ""fullName"": ""Extract candidate full name"",
  ""email"": ""Extract email address"",
  ""phone"": ""Extract phone number"",
  ""skills"": [""List of all skills and technologies extracted""],
  ""education"": [
    {
      ""degree"": ""Degree/Accreditation"",
      ""institution"": ""University/School/Institution"",
      ""year"": ""Graduation Year (YYYY)""
    }
  ],
  ""workExperience"": [
    {
      ""company"": ""Company Name"",
      ""role"": ""Job Title/Role"",
      ""duration"": ""Employment duration (e.g. Jan 2020 - Dec 2022)"",
      ""description"": ""Comprehensive job details and highlights""
    }
  ]
}

Constraints:
1. Extract ALL technical skills, programming languages, frameworks, soft skills, and toolsets found.
2. If any field is not found in the resume, return an empty string or empty array. Do not invent details.
3. Keep the JSON strictly valid. Ensure all brackets and quotes are closed correctly.
";
        }

        private async Task<AffindaResponseDto?> ParsePdfDirectlyAsync(string filePath, CancellationToken ct)
        {
            var fileBytes = await File.ReadAllBytesAsync(filePath, ct);
            var base64Data = Convert.ToBase64String(fileBytes);
            var url = GetGeminiUrl();
            var systemPrompt = GetSystemPrompt();

            var payload = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new object[]
                        {
                            new { inlineData = new { mimeType = "application/pdf", data = base64Data } },
                            new { text = "Extract structured JSON from the attached PDF document following these exact instructions:\n" + systemPrompt }
                        }
                    }
                },
                generationConfig = new
                {
                    responseMimeType = "application/json",
                    temperature = 0.1
                }
            };

            using var client = new HttpClient();
            client.Timeout = TimeSpan.FromSeconds(60); // PDFs can take a bit longer to OCR
            var response = await client.PostAsJsonAsync(url, payload, ct);
            var responseBody = await response.Content.ReadAsStringAsync(ct);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Gemini PDF API error: {StatusCode} - {Error}", response.StatusCode, responseBody);
                return null;
            }

            return ProcessGeminiResponse(responseBody);
        }

        private async Task<AffindaResponseDto?> ParseTextWithGeminiAsync(string text, CancellationToken ct)
        {
            var url = GetGeminiUrl();
            var systemPrompt = GetSystemPrompt();
            var prompt = $"{systemPrompt}\n\nResume Text:\n{text}";

            var payload = new
            {
                contents = new[]
                {
                    new { parts = new[] { new { text = prompt } } }
                },
                generationConfig = new
                {
                    responseMimeType = "application/json",
                    temperature = 0.1
                }
            };

            using var client = new HttpClient();
            var response = await client.PostAsJsonAsync(url, payload, ct);
            var responseBody = await response.Content.ReadAsStringAsync(ct);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Gemini Text API error: {StatusCode} - {Error}", response.StatusCode, responseBody);
                return null;
            }

            return ProcessGeminiResponse(responseBody);
        }

        private AffindaResponseDto? ProcessGeminiResponse(string responseBody)
        {
            try
            {
                using var doc = JsonDocument.Parse(responseBody);
                var generatedText = doc.RootElement
                    .GetProperty("candidates")[0]
                    .GetProperty("content")
                    .GetProperty("parts")[0]
                    .GetProperty("text")
                    .GetString();

                if (string.IsNullOrWhiteSpace(generatedText))
                {
                    _logger.LogError("Gemini returned empty text response.");
                    return null;
                }

                var cleanJson = ExtractJson(generatedText);
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var affindaDto = JsonSerializer.Deserialize<AffindaResponseDto>(cleanJson, options);
                
                if (affindaDto != null)
                {
                    _logger.LogInformation("Successfully parsed resume details via Gemini AI. Skills: {SkillCount}, Experience: {ExpCount}, Education: {EduCount}.", 
                        affindaDto.Skills?.Count ?? 0, affindaDto.WorkExperience?.Count ?? 0, affindaDto.Education?.Count ?? 0);
                }

                return affindaDto;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to parse or deserialize Gemini response. Response body: {Body}", responseBody);
                return null;
            }
        }

        private string ExtractJson(string text)
        {
            if (string.IsNullOrEmpty(text)) return "{}";
            var match = Regex.Match(text, @"\{[\s\S]*\}");
            return match.Success ? match.Value : text;
        }
    }
}
