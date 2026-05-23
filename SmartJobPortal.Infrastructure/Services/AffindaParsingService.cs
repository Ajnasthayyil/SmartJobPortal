using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SmartJobPortal.Application.DTOs.Resume;
using SmartJobPortal.Application.Interfaces;

namespace SmartJobPortal.Infrastructure.Services
{
    public class AffindaParsingService : IAffindaParsingService
    {
        private readonly HttpClient _httpClient;
        private readonly Microsoft.Extensions.Configuration.IConfiguration _configuration;
        private readonly ILogger<AffindaParsingService> _logger;

        public AffindaParsingService(HttpClient httpClient, Microsoft.Extensions.Configuration.IConfiguration configuration, ILogger<AffindaParsingService> logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<AffindaResponseDto?> ParseAsync(string filePath, CancellationToken ct = default)
        {
            try
            {
                if (!File.Exists(filePath))
                    throw new FileNotFoundException($"File not found: {filePath}");

                using var form = new MultipartFormDataContent();
                await using var fileStream = File.OpenRead(filePath);
                
                var fileName = Path.GetFileName(filePath);
                var streamContent = new StreamContent(fileStream);
                
                var ext = Path.GetExtension(filePath).ToLowerInvariant();
                var contentType = ext == ".pdf" ? "application/pdf" : "application/vnd.openxmlformats-officedocument.wordprocessingml.document";
                streamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);
                
                form.Add(streamContent, "file", fileName);
                form.Add(new StringContent("true"), "wait");

                var workspaceId = _configuration["Affinda:WorkspaceId"];
                if (!string.IsNullOrEmpty(workspaceId))
                {
                    form.Add(new StringContent(workspaceId), "workspace");
                }

                var response = await _httpClient.PostAsync("/v3/documents", form, ct);
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorStr = await response.Content.ReadAsStringAsync(ct);
                    _logger.LogError("Affinda API error: {StatusCode} - {Error}", response.StatusCode, errorStr);
                    return null;
                }

                var content = await response.Content.ReadAsStringAsync(ct);
                var doc = JsonDocument.Parse(content);
                var data = doc.RootElement.GetProperty("data");

                var dto = new AffindaResponseDto();

                if (data.TryGetProperty("name", out var nameProp) && nameProp.TryGetProperty("raw", out var nameRaw))
                    dto.FullName = nameRaw.GetString();

                if (data.TryGetProperty("emails", out var emailsProp) && emailsProp.GetArrayLength() > 0)
                    dto.Email = emailsProp[0].GetProperty("parsed").GetString();

                if (data.TryGetProperty("phoneNumbers", out var phoneProp) && phoneProp.GetArrayLength() > 0)
                    dto.Phone = phoneProp[0].GetProperty("parsed").GetString();

                if (data.TryGetProperty("skills", out var skillsProp))
                {
                    foreach (var skill in skillsProp.EnumerateArray())
                    {
                        if (skill.TryGetProperty("parsed", out var parsedSkill))
                        {
                            var s = parsedSkill.GetString();
                            if (!string.IsNullOrEmpty(s)) dto.Skills.Add(s);
                        }
                    }
                }

                if (data.TryGetProperty("education", out var eduProp))
                {
                    foreach (var edu in eduProp.EnumerateArray())
                    {
                        var e = new SmartJobPortal.Application.DTOs.Candidate.EducationDto();
                        if (edu.TryGetProperty("organization", out var org) && org.ValueKind != JsonValueKind.Null)
                            e.Institution = org.GetString();
                        if (edu.TryGetProperty("accreditation", out var acc) && acc.TryGetProperty("education", out var ed) && ed.ValueKind != JsonValueKind.Null)
                            e.Degree = ed.GetString();
                        if (edu.TryGetProperty("dates", out var dates) && dates.ValueKind != JsonValueKind.Null && dates.TryGetProperty("completionDate", out var cd) && cd.ValueKind != JsonValueKind.Null)
                        {
                            var dateStr = cd.GetString();
                            if (!string.IsNullOrEmpty(dateStr) && dateStr.Length >= 4)
                                e.Year = dateStr.Substring(0, 4);
                        }
                        dto.Education.Add(e);
                    }
                }

                if (data.TryGetProperty("workExperience", out var expProp))
                {
                    foreach (var exp in expProp.EnumerateArray())
                    {
                        var e = new SmartJobPortal.Application.DTOs.Candidate.ExperienceDto();
                        if (exp.TryGetProperty("organization", out var org) && org.ValueKind != JsonValueKind.Null)
                            e.Company = org.GetString();
                        if (exp.TryGetProperty("jobTitle", out var title) && title.ValueKind != JsonValueKind.Null)
                            e.Role = title.GetString();
                        if (exp.TryGetProperty("jobDescription", out var desc) && desc.ValueKind != JsonValueKind.Null)
                            e.Description = desc.GetString();
                        dto.WorkExperience.Add(e);
                    }
                }

                return dto;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception calling Affinda parsing service.");
                return null;
            }
        }
    }
}
