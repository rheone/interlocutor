using FluentValidation;

namespace Mediance.Mediation.FluentValidation;

/// <summary>
/// Runs every registered <see cref="IValidator{T}"/> for
/// <typeparamref name="TRequest"/> and short-circuits with
/// <see cref="ValidationFailed"/> before the request reaches authorization,
/// the transaction, or the handler. A no-op if no validator is registered.
/// </summary>
public sealed class ValidationBehavior<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
{
    public async Task<Result<TResponse>> Handle(TRequest request, CancellationToken ct, Func<Task<Result<TResponse>>> next)
    {
        if (!validators.Any())
            return await next();

        var failures = new Dictionary<string, List<(string Code, string Message)>>();
        foreach (var validator in validators)
        {
            foreach (var error in (await validator.ValidateAsync(request, ct)).Errors)
            {
                if (!failures.TryGetValue(error.PropertyName, out var list))
                    failures[error.PropertyName] = list = [];
                list.Add((error.ErrorCode, error.ErrorMessage));
            }
        }

        if (failures.Count == 0)
            return await next();

        return new ValidationFailed(failures.ToDictionary(kv => kv.Key, kv => (IReadOnlyList<(string, string)>)kv.Value));
    }
}

/// <summary>
/// Validates every item of an <see cref="IBulkCommand{TItem,TResponseItem}"/>
/// automatically, using <see cref="IValidator{TItem}"/> resolved from DI.
/// This is the payoff of <see cref="IBulkCommand{TItem,TResponseItem}"/>
/// exposing <c>Items</c> as a real contract member: a consuming application
/// only needs to register <c>IValidator&lt;TItem&gt;</c> for its item type —
/// no hand-written parent validator or <c>RuleForEach</c> wrapper is
/// required unless cross-item rules are also needed, in which case a normal
/// <c>AbstractValidator&lt;TRequest&gt;</c> can be registered alongside
/// this behavior and both will run.
/// </summary>
public sealed class BulkItemValidationBehavior<TRequest, TItem, TResponseItem>(IValidator<TItem> itemValidator)
    : IPipelineBehavior<TRequest, IReadOnlyList<TResponseItem>>
    where TRequest : IBulkCommand<TItem, TResponseItem>
{
    public async Task<Result<IReadOnlyList<TResponseItem>>> Handle(
        TRequest request, CancellationToken ct, Func<Task<Result<IReadOnlyList<TResponseItem>>>> next)
    {
        var failures = new Dictionary<string, List<(string Code, string Message)>>();

        for (var index = 0; index < request.Items.Count; index++)
        {
            var result = await itemValidator.ValidateAsync(request.Items[index], ct);
            foreach (var error in result.Errors)
            {
                // Prefix with the item's index so failures are addressable
                // in a bulk request, e.g. "Items[2].Name" rather than just "Name".
                var key = $"Items[{index}].{error.PropertyName}";
                if (!failures.TryGetValue(key, out var list))
                    failures[key] = list = [];
                list.Add((error.ErrorCode, error.ErrorMessage));
            }
        }

        if (failures.Count == 0)
            return await next();

        return new ValidationFailed(failures.ToDictionary(kv => kv.Key, kv => (IReadOnlyList<(string, string)>)kv.Value));
    }
}
