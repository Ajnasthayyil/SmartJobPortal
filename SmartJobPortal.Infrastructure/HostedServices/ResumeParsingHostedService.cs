using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SmartJobPortal.Application.Interfaces;
using SmartJobPortal.Domain.Entities;

namespace SmartJobPortal.Infrastructure.HostedServices
{
    public class ResumeParsingHostedService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<ResumeParsingHostedService> _logger;
        private readonly TimeSpan _pollInterval = TimeSpan.FromSeconds(30);

        public ResumeParsingHostedService(
            IServiceScopeFactory scopeFactory,
            ILogger<ResumeParsingHostedService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("ResumeParsingHostedService started.");
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var parsedResumeRepo = scope.ServiceProvider.GetRequiredService<IParsedResumeRepository>();
                    var affindaParsingService = scope.ServiceProvider.GetRequiredService<IAffindaParsingService>();

                    var pending = await parsedResumeRepo.GetPendingAsync(batchSize: 5);
                    foreach (var resume in pending)
                    {
                        await ProcessResumeAsync(resume, parsedResumeRepo, affindaParsingService, stoppingToken);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in ResumeParsingHostedService loop.");
                }

                await Task.Delay(_pollInterval, stoppingToken);
            }
            _logger.LogInformation("ResumeParsingHostedService stopping.");
        }

        private async Task ProcessResumeAsync(ParsedResume resume, IParsedResumeRepository parsedResumeRepo, IAffindaParsingService affindaParsingService, CancellationToken ct)
        {
            try
            {
                var result = await affindaParsingService.ParseAsync(resume.FilePath, ct);
                if (result == null)
                {
                    await parsedResumeRepo.UpdateStatusAsync(resume.ParsedResumeId, "Failed", "Affinda returned null");
                    return;
                }

                var json = JsonSerializer.Serialize(result);
                resume.ParsedJson = json;
                resume.Status = "Completed";
                await parsedResumeRepo.UpdateStatusAsync(resume.ParsedResumeId, "Completed");

                if (result.Skills != null && result.Skills.Any())
                {
                    var skills = result.Skills.Select(s => new ResumeSkill { SkillName = s, ProficiencyLevel = "Intermediate" });
                    await parsedResumeRepo.SaveSkillsAsync(resume.ParsedResumeId, skills);
                }

                if (result.Education != null && result.Education.Any())
                {
                    var educations = result.Education.Select(e => new ResumeEducation
                    {
                        Institution = e.Institution,
                        Degree = e.Degree,
                        GraduationYear = e.Year
                    });
                    await parsedResumeRepo.SaveEducationsAsync(resume.ParsedResumeId, educations);
                }

                if (result.WorkExperience != null && result.WorkExperience.Any())
                {
                    var experiences = result.WorkExperience.Select(e => new ResumeExperience
                    {
                        Role = e.Role,
                        Company = e.Company,
                        Duration = e.Duration,
                        Description = e.Description
                    });
                    await parsedResumeRepo.SaveExperiencesAsync(resume.ParsedResumeId, experiences);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process resume {ResumeId}", resume.ParsedResumeId);
                await parsedResumeRepo.UpdateStatusAsync(resume.ParsedResumeId, "Failed", ex.Message);
            }
        }
    }
}
