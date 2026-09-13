namespace Interlocutor.Abstractions.Pipeline;

using System.Runtime.CompilerServices;
using Interlocutor.Abstractions.Markers;
using Interlocutor.Abstractions.Pipeline.Cases;
using Interlocutor.CommonKernel.ResultTypes;

/// <summary>
/// Pipeline behavior that enforces rate limits.
/// </summary>
/// <typeparam name="TRequest">
/// The request type. Constrained to <see cref="IRateLimitedRequest"/> so only requests that declare a limit key
/// close this generic - a container-neutral, compile-time-checked replacement for a runtime <c>is</c> check.
/// </typeparam>
/// <typeparam name="TResponse">
/// The response type, constrained only to <see cref="IUnion"/> - deliberately unconstrained
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
/// (pairing <see cref="IRateLimitedRequest"/> with a response shape that can't express throttling), not a silent
/// failure.
/// </remarks>
public class RateLimitedBehavior<TRequest, TResponse>(IRateLimiter rateLimiter, IOutcomeCaseRegistry caseRegistry)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRateLimitedRequest
    where TResponse : IUnion
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
    /// to construct - pair <see cref="IRateLimitedRequest"/> only with response shapes that include one.
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

        // Within the limit - hand off to the rest of the pipeline (any remaining behaviors, then the handler).
        return await next().ConfigureAwait(false);
    }

    /// <summary>
    /// Constructs the <see cref="RateLimited"/> case of the unconstrained <typeparamref name="TResponse"/>
    /// via <see cref="IOutcomeCaseRegistry"/>, since this type has no compile-time way to build an arbitrary
    /// <see cref="IUnion"/>-shaped response itself.
    /// </summary>
    /// <param name="rateLimited">The rate-limit details to carry in the constructed response.</param>
    /// <returns>The constructed <typeparamref name="TResponse"/> wrapping <paramref name="rateLimited"/>.</returns>
    /// <exception cref="InvalidOperationException">
    /// <typeparamref name="TResponse"/> has no <see cref="RateLimited"/> case to construct.
    /// </exception>
    private TResponse ConstructRateLimitedResponse(RateLimited rateLimited) =>
        caseRegistry.ConstructOrThrow<TResponse, RateLimited>(
            rateLimited,
            $"RateLimitedBehavior<{typeof(TRequest).Name}, {typeof(TResponse).Name}>",
            "throttled a request",
            $"A request implementing {nameof(IRateLimitedRequest)} must use a response type with a {nameof(RateLimited)} case"
        );
}
