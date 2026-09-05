namespace Interlocutor.Abstractions.Pipeline;

using Interlocutor.Abstractions.Markers;
using Interlocutor.Abstractions.Pipeline.Cases;
using Interlocutor.CommonKernel.ResultTypes;

/// <summary>
/// Pipeline behavior that converts cooperative cancellation of the ambient request into the
/// <see cref="Cancelled"/> outcome case.
/// </summary>
/// <typeparam name="TRequest">
/// The request type. Constrained to <see cref="ICancellable"/> so only requests that opt in close this
/// generic - a container-neutral, compile-time-checked replacement for a runtime <c>is</c> check.
/// </typeparam>
/// <typeparam name="TResponse">
/// The response type, constrained only to <see cref="IOutcome"/> - deliberately unconstrained
/// beyond that, matching <see cref="AuthorizationBehavior{TRequest, TResponse}"/>. This behavior's type
/// parameters must map 1:1 onto <see cref="IPipelineBehavior{TRequest, TResponse}"/>'s generic arguments -
/// no extra type parameters - because Microsoft.Extensions.DependencyInjection's open-generic resolver
/// performs positional substitution and cannot resolve an implementation whose type parameters don't match
/// the service's generic arguments 1:1. See the Remarks on <see cref="AuthorizationBehavior{TRequest, TResponse}"/>
/// for the full explanation of that constraint.
/// </typeparam>
/// <remarks>
/// <para>
/// Constructing the <see cref="Cancelled"/> case of an unconstrained <typeparamref name="TResponse"/> can't
/// be done at compile time, so it's delegated to <see cref="IOutcomeCaseRegistry"/> - the same
/// construction/extraction extension point <see cref="AuthorizationBehavior{TRequest, TResponse}"/> uses for
/// <see cref="Forbidden"/>. A <typeparamref name="TResponse"/> with no <see cref="Cancelled"/> case throws a
/// clear, named exception the first time cancellation is actually observed for it - a configuration mistake
/// (pairing <see cref="ICancellable"/> with a response shape that can't express cancellation), not a silent
/// failure.
/// </para>
/// <para>
/// Only an <see cref="OperationCanceledException"/> tied to the ambient <see cref="CancellationToken"/>
/// passed to <see cref="HandleAsync"/> is converted - the catch clause is filtered to
/// <c>when (ct.IsCancellationRequested)</c>. Any other exception, and any
/// <see cref="OperationCanceledException"/> whose cancellation is not tied to that token (for example, one
/// raised by an unrelated <see cref="CancellationTokenSource"/> the handler created internally), propagates
/// unchanged rather than being misreported as the ambient request being cancelled.
/// </para>
/// <para>
/// Because <typeparamref name="TRequest"/> is constrained to <see cref="ICancellable"/>, both Microsoft DI
/// and Autofac skip this open generic registration for a request type that does not implement the marker -
/// the container never even attempts to construct this behavior for it.
/// </para>
/// </remarks>
public class CancellationBehavior<TRequest, TResponse>(IOutcomeCaseRegistry caseRegistry)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : ICancellable
    where TResponse : IOutcome
{
    /// <summary>
    /// Handles the request, converting a cancellation of the ambient <paramref name="ct"/> observed while
    /// awaiting <paramref name="next"/> into a <see cref="Cancelled"/> outcome.
    /// </summary>
    /// <param name="request">The request to process.</param>
    /// <param name="next">The next behavior in the pipeline.</param>
    /// <param name="ct">The ambient cancellation token.</param>
    /// <returns>
    /// A <see cref="Cancelled"/> outcome if <paramref name="next"/> observed cancellation of
    /// <paramref name="ct"/>, otherwise the result from the next behavior or handler.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// Cancellation was observed, but <typeparamref name="TResponse"/> has no <see cref="Cancelled"/> case to
    /// construct - pair <see cref="ICancellable"/> only with response shapes that include one.
    /// </exception>
    public async Task<TResponse> HandleAsync(TRequest request, Func<Task<TResponse>> next, CancellationToken ct)
    {
        try
        {
            return await next().ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            // Only the ambient token's own cancellation is converted - an OperationCanceledException tied to
            // some other token (e.g. an internal timeout the handler applied) is not this request being
            // cancelled, and must propagate unchanged rather than being misreported as Cancelled.
            return ConstructCancelledResponse(default(Cancelled));
        }
    }

    private TResponse ConstructCancelledResponse(Cancelled cancelled) =>
        caseRegistry.ConstructOrThrow<TResponse, Cancelled>(
            cancelled,
            $"CancellationBehavior<{typeof(TRequest).Name}, {typeof(TResponse).Name}>",
            "observed cancellation",
            $"A request implementing {nameof(ICancellable)} must use a response type with a {nameof(Cancelled)} case"
        );
}
