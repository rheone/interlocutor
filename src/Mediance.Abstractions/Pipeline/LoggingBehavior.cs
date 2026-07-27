using System.Diagnostics;
using Mediance.CommonKernel;
using Microsoft.Extensions.Logging;

namespace Mediance.Abstractions.Pipeline;

/// <summary>Pipeline behavior that logs every mediator request passing through the pipeline.</summary>
public class LoggingBehavior<TRequest, TResponse>(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
{
    public async Task<Result<TResponse>> Handle(TRequest request, Func<Task<Result<TResponse>>> next, CancellationToken ct)
    {
        var start = Stopwatch.GetTimestamp();
        var requestType = typeof(TRequest).Name;

        Result<TResponse> result;
        try
        {
            result = await next();
        }
        catch (Exception ex)
        {
            var elapsed = Stopwatch.GetElapsedTime(start);
            logger.LogError(ex, "Request {RequestType} failed with exception after {ElapsedMs}ms", requestType, elapsed.TotalMilliseconds);
            throw;
        }

        var elapsedMs = Stopwatch.GetElapsedTime(start).TotalMilliseconds;
        var outcome = result.Value switch
        {
            TResponse => "Success",
            ValidationFailed => "ValidationFailed",
            RuleViolation => "RuleViolation",
            NotFound => "NotFound",
            Forbidden => "Forbidden",
            Conflict => "Conflict",
            _ => "Unknown",
        };

        logger.LogInformation("Request {RequestType} completed with {Outcome} in {ElapsedMs}ms", requestType, outcome, elapsedMs);
        return result;
    }
}
