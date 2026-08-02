namespace Mediance.Abstractions.Pipeline;

using System.Diagnostics;
using Microsoft.Extensions.Logging;

/// <summary>Pipeline behavior that logs every mediator request passing through the pipeline.</summary>
/// <typeparam name="TRequest">The request type.</typeparam>
/// <typeparam name="TResponse">The response type.</typeparam>
public class LoggingBehavior<TRequest, TResponse>(
    ILogger<LoggingBehavior<TRequest, TResponse>> logger,
    IResponseOutcomeDescriber? describer = null
) : IPipelineBehavior<TRequest, TResponse>
{
    private readonly IResponseOutcomeDescriber describer = describer ?? DefaultResponseOutcomeDescriber.Instance;

    /// <summary>
    /// Handles the request, logging the outcome and elapsed time.
    /// </summary>
    /// <param name="request">The request to process.</param>
    /// <param name="next">The next behavior in the pipeline.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The result from the next behavior or handler.</returns>
    public async Task<TResponse> Handle(TRequest request, Func<Task<TResponse>> next, CancellationToken ct)
    {
        // Capture the start time using Stopwatch for high-resolution timing. This is more accurate than
        // DateTime.UtcNow for elapsed time calculations.
        var start = Stopwatch.GetTimestamp();
        var requestType = typeof(TRequest).Name;

        TResponse result;
        try
        {
            result = await next();
        }
        catch (Exception ex)
        {
            // Handler threw an exception (bug or infrastructure failure, not an expected business outcome).
            // Log it as an error with timing so operators know something went wrong.
            var elapsed = Stopwatch.GetElapsedTime(start);
            logger.LogError(
                ex,
                "Request {RequestType} failed with exception after {ElapsedMs}ms",
                requestType,
                elapsed.TotalMilliseconds
            );
            throw;
        }

        // Handler completed (with or without a business failure outcome). Log the outcome and elapsed time
        // for monitoring and debugging. The describer converts the response to a human-readable label.
        var elapsedMs = Stopwatch.GetElapsedTime(start).TotalMilliseconds;
        var outcome = this.describer.Describe(result);

        logger.LogInformation(
            "Request {RequestType} completed with {Outcome} in {ElapsedMs}ms",
            requestType,
            outcome,
            elapsedMs
        );
        return result;
    }
}
