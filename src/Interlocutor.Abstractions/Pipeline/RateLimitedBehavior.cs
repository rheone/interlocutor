namespace Interlocutor.Abstractions.Pipeline;

using Interlocutor.Abstractions.Markers;
using Interlocutor.Abstractions.Pipeline.Cases;
using Interlocutor.CommonKernel.ResultTypes;

/// <summary>
/// Pipeline behavior that enforces rate limits.
/// </summary>
/// <typeparam name="TRequest">
/// The request type. Constrained to <see cref="IRateLimited"/> so only requests that declare a limit key
/// close this generic - a container-neutral, compile-time-checked replacement for a runtime <c>is</c> check.
/// </typeparam>
/// <typeparam name="TResponse">
/// The response type, constrained only to <see cref="IOutcome"/> - deliberately unconstrained
/// beyond that, mirroring <see cref="AuthorizationBehavior{TRequest, TResponse}"/>: both
/// <typeparamref name="TRequest"/> and <typeparamref name="TResponse"/> map 1:1 onto
/// <see cref="IPipelineBehavior{TRequest, TResponse}"/>'s generic arguments, which is required for
/// Microsoft.Extensions.DependencyInjection's open-generic resolver to construct this behavior at all.
/// </typeparam>
/// <remarks>
/// Constructing the <see cref="RateLimited"/> case of an unconstrained <typeparamref name="TResponse"/>
/// can't be done at compile time, so it's delegated to <see cref="IOutcomeCaseRegistry"/> - the same
/// construction/extraction extension point <see cref="AuthorizationBehavior{TRequest, TResponse}"/> uses for
/// <see cref="Forbidden"/>. A <typeparamref name="TResponse"/> with no <see cref="RateLimited"/> case throws a
/// clear, named exception the first time a request is actually throttled for it - a configuration mistake
/// (pairing <see cref="IRateLimited"/> with a response shape that can't express throttling), not a silent
/// failure.
/// </remarks>
public class RateLimitedBehavior<TRequest, TResponse>(IRateLimiter rateLimiter, IOutcomeCaseRegistry caseRegistry)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRateLimited
    where TResponse : IOutcome
{
    /// <summary>
    /// Handles the request, checking the rate limit and short-circuiting with <see cref="RateLimited"/>
    /// if it's exceeded.
    /// </summary>
    /// <param name="request">The request to process.</param>
    /// <param name="next">The next behavior in the pipeline.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A <see cref="RateLimited"/> outcome if the limit was exceeded, otherwise the result from the next behavior or handler.</returns>
    /// <exception cref="InvalidOperationException">
    /// The request was throttled, but <typeparamref name="TResponse"/> has no <see cref="RateLimited"/> case
    /// to construct - pair <see cref="IRateLimited"/> only with response shapes that include one.
    /// </exception>
    public async Task<TResponse> HandleAsync(TRequest request, Func<Task<TResponse>> next, CancellationToken ct)
    {
        // Check the rate limit. If it's exceeded, short-circuit immediately with RateLimited without
        // invoking the handler or any downstream behaviors.
        var check = await rateLimiter.CheckAsync(request.LimitKey, request, ct).ConfigureAwait(false);
        if (!check.IsAllowed)
        {
            var rateLimited = new RateLimited(request.LimitKey, check.RetryAfter);
            return ConstructRateLimitedResponse(rateLimited);
        }

        // Within the limit - proceed to the handler.
        return await next().ConfigureAwait(false);
    }

    private TResponse ConstructRateLimitedResponse(RateLimited rateLimited) =>
        caseRegistry.ConstructOrThrow<TResponse, RateLimited>(
            rateLimited,
            $"RateLimitedBehavior<{typeof(TRequest).Name}, {typeof(TResponse).Name}>",
            "throttled a request",
            $"A request implementing {nameof(IRateLimited)} must use a response type with a {nameof(RateLimited)} case"
        );
}
