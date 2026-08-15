namespace Interlocutor.Abstractions.Pipeline;

using Interlocutor.Abstractions.Markers;
using Interlocutor.CommonKernel;
using Interlocutor.CommonKernel.ResultTypes;

/// <summary>
/// Pipeline behavior that enforces declarative authorization policies.
/// </summary>
/// <typeparam name="TRequest">
/// The request type. Constrained to <see cref="IRequiresPolicy"/> so only requests that declare a policy
/// close this generic — a container-neutral, compile-time-checked replacement for a runtime <c>is</c> check.
/// </typeparam>
/// <typeparam name="TSuccess">The success case type of the result.</typeparam>
/// <remarks>
/// <para>
/// Short-circuiting behaviors must be able to construct their failure case without knowing the
/// handler's response shape in general, so this behavior is also constrained to responses of the
/// concrete shape <see cref="OperationOutcome{TSuccess, TFailureResult}"/> with <see cref="Forbidden"/> as
/// the failure case. Handlers using a different response shape need a bespoke authorization
/// behavior instead. The two constraints — request shape and response shape — are independent and both apply.
/// </para>
/// <para>
/// Because <typeparamref name="TRequest"/> is constrained to <see cref="IRequiresPolicy"/>, both Microsoft
/// DI and Autofac skip this open generic registration entirely for a request type that does not implement the
/// marker (or whose response is not shaped <see cref="OperationOutcome{TSuccess, Forbidden}"/>) — the
/// container never even attempts to construct this behavior for it.
/// </para>
/// </remarks>
public class AuthorizationBehavior<TRequest, TSuccess>(IPolicyEvaluator policyEvaluator)
    : IPipelineBehavior<TRequest, OperationOutcome<TSuccess, Forbidden>>
    where TRequest : IRequiresPolicy
{
    /// <summary>
    /// Handles the request, evaluating the authorization policy and short-circuiting with <see cref="Forbidden"/>
    /// if it fails.
    /// </summary>
    /// <param name="request">The request to process.</param>
    /// <param name="next">The next behavior in the pipeline.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A <see cref="Forbidden"/> outcome if authorization fails, otherwise the result from the next behavior or handler.</returns>
    public async Task<OperationOutcome<TSuccess, Forbidden>> Handle(
        TRequest request,
        Func<Task<OperationOutcome<TSuccess, Forbidden>>> next,
        CancellationToken ct
    )
    {
        // Evaluate the authorization policy. If it fails, short-circuit immediately with Forbidden
        // without invoking the handler or any downstream behaviors.
        var authorized = await policyEvaluator.EvaluateAsync(request.PolicyName, request, ct);
        if (!authorized)
        {
            return new Forbidden($"Policy '{request.PolicyName}' denied access to {typeof(TRequest).Name}");
        }

        // Authorization passed — proceed to the handler.
        return await next();
    }
}
