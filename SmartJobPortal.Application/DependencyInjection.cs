using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using SmartJobPortal.Application.Common.Behaviors;
using System.Reflection;

namespace SmartJobPortal.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(assembly);
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(PerformanceBehavior<,>));
        });

        services.AddValidatorsFromAssembly(assembly);

        // Common Utilities
        services.AddSingleton<Common.Utilities.ISemanticMatcher, Common.Utilities.SemanticMatcher>();

        return services;
    }
}
