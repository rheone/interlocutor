namespace Mediance.Abstractions.Pipeline;

using Mediance.Abstractions.Markers;
using Mediance.CommonKernel;
using Mediance.CommonKernel.ResultTypes;

/// <summary>
/// Pipeline behavior that enforces declarative authorization policies on requests implementing
/// <see cref="IRequiresPolicy"/>.
/// </summary>
/// <remarks>
/// Short-circuiting behaviors must be able to construct their failure case without knowing the
/// handler's response shape in general, so this behavior is constrained to responses of the
/// concrete shape <see cref="Result{TSuccess, TFailureResult}"/> with <see cref="Forbidden"/> as
/// the failure case. Handlers using a different response shape need a bespoke authorization
/// behavior instead.
/// </remarks>
public class AuthorizationBehavior<TRequest, TSuccess>(IPolicyEvaluator policyEvaluator)
    : IPipelineBehavior<TRequest, Result<TSuccess, Forbidden>>
{
    public async Task<Result<TSuccess, Forbidden>> Handle(
        TRequest request,
        Func<Task<Result<TSuccess, Forbidden>>> next,
        CancellationToken ct
    )
    {
        if (request is not IRequiresPolicy requiresPolicy)
        {
            return await next();
        }

        var authorized = await policyEvaluator.EvaluateAsync(requiresPolicy.PolicyName, request, ct);
        if (!authorized)
        {
            return new Forbidden($"Policy '{requiresPolicy.PolicyName}' denied access to {typeof(TRequest).Name}");
        }

        return await next();
    }
}
