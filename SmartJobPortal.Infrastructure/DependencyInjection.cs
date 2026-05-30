using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SmartJobPortal.Application.Interfaces;
using SmartJobPortal.Infrastructure.Data;
using SmartJobPortal.Infrastructure.Repositories;
using SmartJobPortal.Infrastructure.Services;
using SmartJobPortal.Infrastructure.HostedServices;

namespace SmartJobPortal.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // ── Data & Connection Factory ────────────────────────────────
        services.AddScoped<IDbConnectionFactory>(sp =>
            new DbConnectionFactory(configuration));

        // ── Repositories ─────────────────────────────────────────────
        services.AddScoped<ICandidateRepository, CandidateRepository>();
        services.AddScoped<IJobRepository, JobRepository>();
        services.AddScoped<IApplicationRepository, ApplicationRepository>();
        services.AddScoped<IMatchScoreRepository, MatchScoreRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRecruiterRepository, RecruiterRepository>();
        services.AddScoped<IRecruiterJobRepository, RecruiterJobRepository>();
        services.AddScoped<IAdminRepository, AdminRepository>();
        services.AddScoped<INotificationRepository, NotificationRepository>();
        services.AddScoped<IPostRepository, PostRepository>();
        services.AddScoped<IChatbotRepository, ChatbotRepository>();

        // ── Services ────────────────────────────────────────────────
        services.AddScoped<IChatbotService, ChatbotService>();
        services.AddScoped<IPhotoService, PhotoService>();
        services.AddScoped<IJwtService, JwtService>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<ICloudinaryService, CloudinaryService>();
        services.AddScoped<IEmailService, SmtpEmailService>();

        
        // External AI Services
        // Register Affinda parsing service and its HttpClient
        services.AddHttpClient<IAffindaParsingService, AffindaParsingService>(c =>
        {
            c.BaseAddress = new Uri(configuration["Affinda:BaseUrl"] ?? "https://api.affinda.com");
            c.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", configuration["Affinda:ApiKey"]);
        });
        // Register recruitment parsing repositories
        services.AddScoped<IParsedResumeRepository, ParsedResumeRepository>();
        // Register background parsing hosted service
        services.AddHostedService<ResumeParsingHostedService>();
        return services;
    }
}
