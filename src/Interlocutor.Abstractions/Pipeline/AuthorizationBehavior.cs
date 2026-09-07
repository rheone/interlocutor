namespace Interlocutor.Abstractions.Pipeline;

using System.Runtime.CompilerServices;
using Interlocutor.Abstractions.Markers;
using Interlocutor.Abstractions.Pipeline.Cases;
using Interlocutor.CommonKernel.ResultTypes;

/// <summary>
/// Pipeline behavior that enforces declarative authorization policies.
/// </summary>
/// <typeparam name="TRequest">
/// The request type. Constrained to <see cref="IRequiresPolicyRequest"/> so only requests that declare a policy
/// close this generic - a container-neutral, compile-time-checked replacement for a runtime <c>is</c> check.
/// </typeparam>
/// <typeparam name="TResponse">
/// The response type, constrained only to <see cref="IUnion"/> - deliberately unconstrained
/// beyond that. See the Remarks for why.
/// </typeparam>
/// <remarks>
/// <para>
/// <b>Why <typeparamref name="TResponse"/> isn't decomposed into a success-type parameter with
/// <see cref="Forbidden"/> pinned as the failure case:</b> an earlier version of this type was
/// <c>AuthorizationBehavior&lt;TRequest, TSuccess&gt; : IPipelineBehavior&lt;TRequest,
/// Outcome&lt;TSuccess, Forbidden&gt;&gt;</c> - compile-time safe (the response shape was guaranteed
/// to have a <see cref="Forbidden"/> case), but unusable on Microsoft.Extensions.DependencyInjection. That
/// container's open-generic resolver performs positional substitution
/// (<c>implementationType.MakeGenericType(serviceType's generic arguments)</c>) and requires an
/// implementation's type parameters to map 1:1 onto the interface's generic arguments - exactly what every
/// other built-in behavior does (<c>LoggingBehavior&lt;TRequest,TResponse&gt; : IPipelineBehavior&lt;TRequest,
/// TResponse&gt;</c>), and exactly what the old shape did not, since <c>TSuccess</c> was nested inside
/// <c>Outcome&lt;TSuccess, Forbidden&gt;</c> rather than mapped directly. That mismatch made
/// <c>GetServices&lt;IPipelineBehavior&lt;TRequest,TResponse&gt;&gt;()</c> - what <see cref="Interlocutor.Abstractions.Mediator"/> calls
/// on every dispatch - throw <see cref="ArgumentException"/> unconditionally, for every request, the instant
/// this behavior was registered. Autofac's <c>RegisterGeneric</c> performs real structural unification and
/// tolerated the old shape, which is why this went uncaught until a genuine Microsoft DI resolution was
/// attempted rather than only a registration-list check.
/// </para>
/// <para>
/// <b>PROTOTYPE (spike/eject-withcase-case-fidelity):</b> this behavior no longer constructs its own
/// <see cref="Forbidden"/> response and takes no <see cref="IOutcomeCaseRegistry"/> dependency at all - it
/// ejects the bare <see cref="Forbidden"/> case value via <see cref="Eject.WithCase{TCase}(TCase)"/> and lets
/// the pipeline's dispatch machinery construct the concrete <typeparamref name="TResponse"/> at the one place
/// that already knows what it is. A <typeparamref name="TResponse"/> with no <see cref="Forbidden"/> case
/// still throws a clear, named exception the first time authorization is actually denied for it - a
/// configuration mistake (pairing <see cref="IRequiresPolicyRequest"/> with a response shape that can't
/// express denial), not a silent failure - but that exception is now thrown by the catch site that attempts
/// the construction, not by this behavior. See <see cref="Eject.WithCase{TCase}(TCase)"/> and
/// <see cref="PipelineEjectCaseException"/>'s remarks for the full mechanism this is the worked example for.
/// </para>
/// <para>
/// Because <typeparamref name="TRequest"/> is constrained to <see cref="IRequiresPolicyRequest"/>, both Microsoft
/// DI and Autofac skip this open generic registration for a request type that does not implement the marker
/// - the container never even attempts to construct this behavior for it.
/// </para>
/// </remarks>
public class AuthorizationBehavior<TRequest, TResponse>(IPolicyEvaluator policyEvaluator)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequiresPolicyRequest
    where TResponse : IUnion
{
    /// <summary>
    /// Handles the request, evaluating the authorization policy and short-circuiting with <see cref="Forbidden"/>
    /// if it fails.
    /// </summary>
    /// <param name="request">The request to process.</param>
    /// <param name="next">The next behavior in the pipeline.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The result from the next behavior or handler, if authorization passes.</returns>
    /// <exception cref="PipelineEjectCaseException">
    /// Authorization was denied - always thrown in that case, carrying the ejected <see cref="Forbidden"/>
    /// case value. Caught by the pipeline's dispatch machinery, never observed by callers of the mediator; if
    /// <typeparamref name="TResponse"/> has no <see cref="Forbidden"/> case to construct, the catch site
    /// throws a named <see cref="InvalidOperationException"/> instead of returning this exception's response.
    /// </exception>
    public async Task<TResponse> HandleAsync(TRequest request, Func<Task<TResponse>> next, CancellationToken ct)
    {
        // Evaluate the authorization policy. If it fails, short-circuit immediately with Forbidden
        // without invoking the handler or any downstream behaviors - deferring construction of the concrete
        // TResponse to the pipeline's dispatch machinery, which is the only place that knows what it is.
        var authorized = await policyEvaluator.EvaluateAsync(request.PolicyName, request, ct).ConfigureAwait(false);
        if (!authorized)
        {
            throw Eject.WithCase(new Forbidden($"Policy '{request.PolicyName}' denied access to {typeof(TRequest).Name}"));
        }

        // Authorization passed - proceed to the handler.
        return await next().ConfigureAwait(false);
    }
}
