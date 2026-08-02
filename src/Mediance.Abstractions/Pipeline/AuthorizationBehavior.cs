namespace Mediance.Abstractions.Pipeline;

using Mediance.Abstractions.Markers;
using Mediance.CommonKernel;
using Mediance.CommonKernel.ResultTypes;

/// <summary>
/// Pipeline behavior that enforces declarative authorization policies on requests implementing
/// <see cref="IRequiresPolicy"/>.
/// </summary>
/// <typeparam name="TRequest">The request type.</typeparam>
/// <typeparam name="TSuccess">The success case type of the result.</typeparam>
/// <remarks>
/// Short-circuiting behaviors must be able to construct their failure case without knowing the
/// handler's response shape in general, so this behavior is constrained to responses of the
/// concrete shape <see cref="OperationOutcome{TSuccess, TFailureResult}"/> with <see cref="Forbidden"/> as
/// the failure case. Handlers using a different response shape need a bespoke authorization
/// behavior instead.
/// </remarks>
public class AuthorizationBehavior<TRequest, TSuccess>(IPolicyEvaluator policyEvaluator)
    : IPipelineBehavior<TRequest, OperationOutcome<TSuccess, Forbidden>>
{
    public async Task<OperationOutcome<TSuccess, Forbidden>> Handle(
        TRequest request,
        Func<Task<OperationOutcome<TSuccess, Forbidden>>> next,
        CancellationToken ct
    )
    {
        // If the request doesn't require authorization, skip this behavior entirely.
        if (request is not IRequiresPolicy requiresPolicy)
        {
            return await next();
        }

        // Evaluate the authorization policy. If it fails, short-circuit immediately with Forbidden
        // without invoking the handler or any downstream behaviors.
        var authorized = await policyEvaluator.EvaluateAsync(requiresPolicy.PolicyName, request, ct);
        if (!authorized)
        {
            return new Forbidden($"Policy '{requiresPolicy.PolicyName}' denied access to {typeof(TRequest).Name}");
        }

        // Authorization passed — proceed to the handler.
        return await next();
    }
}
