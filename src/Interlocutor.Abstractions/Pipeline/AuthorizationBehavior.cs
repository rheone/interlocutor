namespace Interlocutor.Abstractions.Pipeline;

using Interlocutor.Abstractions.Markers;
using Interlocutor.Abstractions.Pipeline.Cases;
using Interlocutor.CommonKernel.ResultTypes;

/// <summary>
/// Pipeline behavior that enforces declarative authorization policies.
/// </summary>
/// <typeparam name="TRequest">
/// The request type. Constrained to <see cref="IRequiresPolicy"/> so only requests that declare a policy
/// close this generic - a container-neutral, compile-time-checked replacement for a runtime <c>is</c> check.
/// </typeparam>
/// <typeparam name="TResponse">
/// The response type, constrained only to <see cref="IOperationOutcome"/> - deliberately unconstrained
/// beyond that. See the Remarks for why.
/// </typeparam>
/// <remarks>
/// <para>
/// <b>Why <typeparamref name="TResponse"/> isn't decomposed into a success-type parameter with
/// <see cref="Forbidden"/> pinned as the failure case:</b> an earlier version of this type was
/// <c>AuthorizationBehavior&lt;TRequest, TSuccess&gt; : IPipelineBehavior&lt;TRequest,
/// OperationOutcome&lt;TSuccess, Forbidden&gt;&gt;</c> - compile-time safe (the response shape was guaranteed
/// to have a <see cref="Forbidden"/> case), but unusable on Microsoft.Extensions.DependencyInjection. That
/// container's open-generic resolver performs positional substitution
/// (<c>implementationType.MakeGenericType(serviceType's generic arguments)</c>) and requires an
/// implementation's type parameters to map 1:1 onto the interface's generic arguments - exactly what every
/// other built-in behavior does (<c>LoggingBehavior&lt;TRequest,TResponse&gt; : IPipelineBehavior&lt;TRequest,
/// TResponse&gt;</c>), and exactly what the old shape did not, since <c>TSuccess</c> was nested inside
/// <c>OperationOutcome&lt;TSuccess, Forbidden&gt;</c> rather than mapped directly. That mismatch made
/// <c>GetServices&lt;IPipelineBehavior&lt;TRequest,TResponse&gt;&gt;()</c> - what <see cref="Interlocutor.Abstractions.Mediator"/> calls
/// on every dispatch - throw <see cref="ArgumentException"/> unconditionally, for every request, the instant
/// this behavior was registered. Autofac's <c>RegisterGeneric</c> performs real structural unification and
/// tolerated the old shape, which is why this went uncaught until a genuine Microsoft DI resolution was
/// attempted rather than only a registration-list check.
/// </para>
/// <para>
/// The trade-off: constructing the <see cref="Forbidden"/> case of an unconstrained <typeparamref name="TResponse"/>
/// can't be done at compile time, so it's delegated to <see cref="IOutcomeCaseRegistry"/> - the same
/// construction/extraction extension point every hand-rolled, non-union <see cref="IOperationOutcome"/>
/// implementation participates in. A <typeparamref name="TResponse"/> with no <see cref="Forbidden"/> case
/// throws a clear, named exception the first time authorization is actually denied for it - a configuration
/// mistake (pairing <see cref="IRequiresPolicy"/> with a response shape that can't express denial), not a
/// silent failure.
/// </para>
/// <para>
/// Because <typeparamref name="TRequest"/> is constrained to <see cref="IRequiresPolicy"/>, both Microsoft
/// DI and Autofac skip this open generic registration for a request type that does not implement the marker
/// - the container never even attempts to construct this behavior for it.
/// </para>
/// </remarks>
public class AuthorizationBehavior<TRequest, TResponse>(IPolicyEvaluator policyEvaluator, IOutcomeCaseRegistry caseRegistry)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequiresPolicy
    where TResponse : IOperationOutcome
{
    /// <summary>
    /// Handles the request, evaluating the authorization policy and short-circuiting with <see cref="Forbidden"/>
    /// if it fails.
    /// </summary>
    /// <param name="request">The request to process.</param>
    /// <param name="next">The next behavior in the pipeline.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A <see cref="Forbidden"/> outcome if authorization fails, otherwise the result from the next behavior or handler.</returns>
    /// <exception cref="InvalidOperationException">
    /// Authorization was denied, but <typeparamref name="TResponse"/> has no <see cref="Forbidden"/> case to
    /// construct - pair <see cref="IRequiresPolicy"/> only with response shapes that include one.
    /// </exception>
    public async Task<TResponse> Handle(TRequest request, Func<Task<TResponse>> next, CancellationToken ct)
    {
        // Evaluate the authorization policy. If it fails, short-circuit immediately with Forbidden
        // without invoking the handler or any downstream behaviors.
        var authorized = await policyEvaluator.EvaluateAsync(request.PolicyName, request, ct);
        if (!authorized)
        {
            var forbidden = new Forbidden($"Policy '{request.PolicyName}' denied access to {typeof(TRequest).Name}");
            return ConstructForbiddenResponse(forbidden);
        }

        // Authorization passed - proceed to the handler.
        return await next();
    }

    private TResponse ConstructForbiddenResponse(Forbidden forbidden) =>
        caseRegistry.ConstructOrThrow<TResponse, Forbidden>(
            forbidden,
            $"AuthorizationBehavior<{typeof(TRequest).Name}, {typeof(TResponse).Name}>",
            "denied a request",
            $"A request implementing {nameof(IRequiresPolicy)} must use a response type with a {nameof(Forbidden)} case"
        );
}
