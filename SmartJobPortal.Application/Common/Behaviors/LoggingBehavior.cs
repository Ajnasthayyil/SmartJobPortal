using MediatR;
using Microsoft.Extensions.Logging;

namespace SmartJobPortal.Application.Common.Behaviors;

public class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;

    public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;

        _logger.LogInformation("SmartJobPortal Request: {Name} {@Request}", requestName, request);

        var response = await next();

        _logger.LogInformation("SmartJobPortal Response: {Name} {@Response}", requestName, response);

        return response;
    }
}
