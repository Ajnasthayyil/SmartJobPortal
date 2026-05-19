using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SmartJobPortal.Application.Interfaces;
using SmartJobPortal.Infrastructure.Data;
using SmartJobPortal.Infrastructure.Repositories;
using SmartJobPortal.Infrastructure.Services;

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

        // ── Services ────────────────────────────────────────────────
        services.AddScoped<IPhotoService, PhotoService>();
        services.AddScoped<IJwtService, JwtService>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<ICloudinaryService, CloudinaryService>();
        
        // External AI Services
        services.AddHttpClient<IHuggingFaceService, HuggingFaceParserService>();

        return services;
    }
}
