namespace Interlocutor.Abstractions.Pipeline;

using System.Diagnostics;
using Interlocutor.Abstractions.Pipeline.Classifiers;
using Microsoft.Extensions.Logging;

/// <summary>Pipeline behavior that logs every mediator request passing through the pipeline.</summary>
/// <typeparam name="TRequest">The request type.</typeparam>
/// <typeparam name="TResponse">
/// The response type produced for <typeparamref name="TRequest"/>. Unconstrained here, unlike behaviors
/// that construct a specific outcome case: <see cref="IResponseOutcomeDescriber.Describe(object?)"/> takes
/// the response as a plain <see cref="object"/>, so this behavior never needs to know its concrete shape.
/// </typeparam>
/// <param name="logger">The logger this behavior writes request-completion and failure entries to.</param>
/// <param name="describer">
/// Produces the outcome label recorded on successful completion. When omitted, falls back to a
/// <see cref="DefaultResponseOutcomeDescriber"/> built from <see cref="Classifiers.OutcomeClassifierBuilder.DefaultClassifier"/>.
/// </param>
public class LoggingBehavior<TRequest, TResponse>(
    ILogger<LoggingBehavior<TRequest, TResponse>> logger,
    IResponseOutcomeDescriber? describer = null
) : IPipelineBehavior<TRequest, TResponse>
{
    private readonly IResponseOutcomeDescriber describer =
        describer ?? new DefaultResponseOutcomeDescriber(Classifiers.OutcomeClassifierBuilder.DefaultClassifier);

    /// <summary>
    /// Handles the request, logging the outcome and elapsed time.
    /// </summary>
    /// <param name="request">The request to process.</param>
    /// <param name="next">The next behavior in the pipeline.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The result from the next behavior or handler, returned unchanged after being logged.</returns>
    public async Task<TResponse> HandleAsync(TRequest request, Func<Task<TResponse>> next, CancellationToken ct)
    {
        // Capture the start time using Stopwatch for high-resolution timing. This is more accurate than
        // DateTime.UtcNow for elapsed time calculations.
        var start = Stopwatch.GetTimestamp();
        var requestType = typeof(TRequest).Name;

        TResponse result;
        try
        {
            result = await next().ConfigureAwait(false);
        }
        // S2139 wants either logging or rethrowing, not both, on the theory that a caller further up the
        // stack will log the same exception again. Nothing further out in this pipeline does: the exception
        // simply propagates out of Send/Publish to the caller (see CLAUDE.md - exceptions here mean bugs or
        // infra faults, not an expected outcome the pipeline itself handles), and this catch attaches
        // context - which request type, how long it ran before failing - that no generic upstream handler
        // could reconstruct. Rethrowing via a bare `throw;` preserves the original stack trace.
#pragma warning disable S2139
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
#pragma warning restore S2139

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
