using FluentValidation;
using Mediance.Abstractions;
using Mediance.Abstractions.Pipeline;
using Mediance.CommonKernel;

namespace Mediance.FluentValidation;

/// <summary>
/// A pipeline behavior that validates a request using all registered <see cref="IValidator{TRequest}"/> instances
/// before passing it to the next handler in the pipeline.
/// </summary>
/// <typeparam name="TRequest">The type of the request being validated.</typeparam>
/// <typeparam name="TResponse">The type of the response produced by the request handler.</typeparam>
/// <param name="validators">The set of FluentValidation validators to validate the request.
/// If this collection is empty, validation is skipped and the request is forwarded directly
/// to the next pipeline stage.</param>
/// <remarks>
/// <para>
/// Validation errors from all validators are aggregated by property name. Each property maps to a list
/// of <c>(ErrorCode, ErrorMessage)</c> tuples. This preserves every violation from every validator
/// rather than stopping at the first failure.
/// </para>
/// <para>
/// If validation succeeds (no failures across any validator), the request is forwarded to the next
/// pipeline stage. If any validation rule is violated, a <see cref="Result{TResponse}"/> containing
/// a <see cref="ValidationFailed"/> failure is returned without invoking the inner handler.
/// </para>
/// </remarks>
public sealed class ValidationBehavior<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
{
    /// <summary>
    /// Validates the request using all registered validators. If validation succeeds,
    /// the request is forwarded to the next pipeline stage.
    /// </summary>
    /// <param name="request">The request to validate.</param>
    /// <param name="next">The next delegate in the pipeline, invoked if validation passes.</param>
    /// <param name="ct">A cancellation token to cancel validation or the inner handler.</param>
    /// <returns>A <see cref="Result{TResponse}"/> that is either a success result from the inner handler
    /// or a <see cref="ValidationFailed"/> failure containing all validation errors from all validators.</returns>
    public async Task<Result<TResponse>> Handle(TRequest request, Func<Task<Result<TResponse>>> next, CancellationToken ct)
    {
        // Short-circuit when no validators are registered — the pipeline proceeds without validation.
        if (!validators.Any())
        {
            return await next();
        }

        // Aggregate all validation failures across every validator, keyed by property name.
        // A single property may accumulate violations from multiple validators.
        var failures = new Dictionary<string, List<(string Code, string Message)>>();
        foreach (var validator in validators)
        {
            foreach (var error in (await validator.ValidateAsync(request, ct)).Errors)
            {
                if (!failures.TryGetValue(error.PropertyName, out var list))
                {
                    failures[error.PropertyName] = list = [];
                }

                list.Add((error.ErrorCode, error.ErrorMessage));
            }
        }

        if (failures.Count == 0)
        {
            return await next();
        }

        // Convert the mutable list values to read-only for the ValidationFailed result.
        return new Result<TResponse>(
            new ValidationFailed(failures.ToDictionary(kv => kv.Key, kv => (IReadOnlyList<(string, string)>)kv.Value))
        );
    }
}
