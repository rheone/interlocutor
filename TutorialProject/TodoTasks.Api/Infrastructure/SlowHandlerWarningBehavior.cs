namespace TodoTasks.Api.Infrastructure;

using System.Diagnostics;
using Interlocutor.Abstractions.Pipeline;

/// <summary>
/// A custom <see cref="IPipelineBehavior{TRequest, TResponse}"/> that warns when a handler alone takes
/// longer than <see cref="Threshold"/> - not the whole dispatch, just the handler's own work.
/// </summary>
/// <typeparam name="TRequest">The request type this behavior processes.</typeparam>
/// <typeparam name="TResponse">The response type returned after pipeline execution.</typeparam>
/// <param name="logger">The logger this behavior warns through.</param>
/// <remarks>
/// Backlog ticket 17 - <c>documentation/beginners-guide-todo-api.md</c> Step 11's own worked example of a
/// custom behavior. What this measures is decided entirely by registration position, not by anything
/// inside this type: registered <em>innermost</em> - the last <c>AddBehavior</c> call after
/// <c>UseRecommendedChain()</c>'s five behaviors - it wraps nothing but the handler itself, so its
/// stopwatch starts after logging, auditing, authorization, validation, and the transaction's own begin
/// have already run, and stops before any of their egress work. Registered anywhere else in the chain, it
/// would measure that surrounding cost too - see <c>SlowHandlerWarningBehaviorTests</c> (in
/// <c>TodoTasks.Api.Tests</c>) for a test proving that isolation directly, and the guide section above
/// for why this type is deliberately not wired into this project's own <c>Program.cs</c>: it
/// demonstrates composition, not something the finished project needs.
/// </remarks>
public sealed class SlowHandlerWarningBehavior<TRequest, TResponse>(
    ILogger<SlowHandlerWarningBehavior<TRequest, TResponse>> logger
) : IPipelineBehavior<TRequest, TResponse>
{
    private static readonly TimeSpan Threshold = TimeSpan.FromMilliseconds(250);

    /// <inheritdoc/>
    public async Task<TResponse> Handle(TRequest request, Func<Task<TResponse>> next, CancellationToken ct)
    {
        var stopwatch = Stopwatch.StartNew(); // Ingress - runs after whatever's registered ahead of this one.
        var response = await next().ConfigureAwait(false);
        stopwatch.Stop(); // Egress - runs before any of those behaviors' own egress.

        if (stopwatch.Elapsed > Threshold)
        {
            logger.LogWarning(
                "{RequestType} took {ElapsedMilliseconds}ms - past the {ThresholdMilliseconds}ms threshold",
                typeof(TRequest).Name,
                stopwatch.ElapsedMilliseconds,
                Threshold.TotalMilliseconds
            );
        }

        return response;
    }
}
