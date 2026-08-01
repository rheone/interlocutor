using Mediance.Abstractions.Markers;
using Mediance.CommonKernel.ResultTypes;

namespace Mediance.Abstractions.Pipeline;

/// <summary>Pipeline behavior that enforces declarative authorization policies on requests implementing <see cref="IRequiresPolicy"/>.</summary>
public class AuthorizationBehavior<TRequest, TResponse>(IPolicyEvaluator policyEvaluator)
    : IPipelineBehavior<TRequest, TResponse>
{
    public async Task<Result<TResponse>> Handle(TRequest request, Func<Task<Result<TResponse>>> next, CancellationToken ct)
    {
        if (request is not IRequiresPolicy requiresPolicy)
        {
            return await next();
        }

        var authorized = await policyEvaluator.EvaluateAsync(requiresPolicy.PolicyName, request, ct);
        if (!authorized)
        {
            return new Result<TResponse>(
                new Forbidden($"Policy '{requiresPolicy.PolicyName}' denied access to {typeof(TRequest).Name}")
            );
        }

        return await next();
    }
}
