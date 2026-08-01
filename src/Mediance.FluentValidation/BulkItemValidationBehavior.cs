using FluentValidation;
using Mediance.Abstractions.Commands;
using Mediance.Abstractions.Pipeline;
using Mediance.CommonKernel;

namespace Mediance.FluentValidation;

/// <summary>
/// A pipeline behavior that validates each item in a bulk command independently before
/// passing execution to the next handler in the pipeline.
/// </summary>
/// <typeparam name="TRequest">The type of the bulk command. Must implement <see cref="IBulkCommand{TItem,TResponseItem}"/>.</typeparam>
/// <typeparam name="TItem">The type of each individual item in the bulk operation.</typeparam>
/// <typeparam name="TResponseItem">The type of each individual response item produced by the handler.</typeparam>
/// <param name="itemValidator">The FluentValidation validator used to validate each item in the batch.</param>
/// <remarks>
/// <para>
/// Each item in <c>TRequest.Items</c> is validated independently against the <paramref name="itemValidator"/>.
/// Validation errors are indexed by position using keys formatted as <c>Items[{index}].{PropertyName}</c>,
/// allowing consumers to identify which specific item caused each failure.
/// </para>
/// <para>
/// If all items pass validation, the request is forwarded to the next pipeline stage. If any item fails,
/// a <see cref="ValidationFailed"/> failure is returned without invoking the inner handler.
/// </para>
/// </remarks>
public sealed class BulkItemValidationBehavior<TRequest, TItem, TResponseItem>(IValidator<TItem> itemValidator)
    : IPipelineBehavior<TRequest, Result<IReadOnlyList<TResponseItem>, ValidationFailed>>
    where TRequest : IBulkCommand<TItem, TResponseItem>
{
    /// <summary>
    /// Validates each item in the bulk command. If all items pass validation,
    /// the request is forwarded to the next pipeline stage.
    /// </summary>
    /// <param name="request">The bulk command whose items will be validated.</param>
    /// <param name="next">The next delegate in the pipeline, invoked if all items pass validation.</param>
    /// <param name="ct">A cancellation token to cancel validation or the inner handler.</param>
    /// <returns>Either the success result from the inner handler, or a <see cref="ValidationFailed"/>
    /// failure containing indexed validation errors per item.</returns>
    public async Task<Result<IReadOnlyList<TResponseItem>, ValidationFailed>> Handle(
        TRequest request,
        Func<Task<Result<IReadOnlyList<TResponseItem>, ValidationFailed>>> next,
        CancellationToken ct
    )
    {
        var failures = new Dictionary<string, List<(string Code, string Message)>>();

        // Validate each item independently and collect errors with an index-based key
        // so the caller can identify which item in the batch caused each failure.
        for (var index = 0; index < request.Items.Count; index++)
        {
            var result = await itemValidator.ValidateAsync(request.Items[index], ct);
            foreach (var error in result.Errors)
            {
                var key = $"Items[{index}].{error.PropertyName}";
                if (!failures.TryGetValue(key, out var list))
                {
                    failures[key] = list = [];
                }

                list.Add((error.ErrorCode, error.ErrorMessage));
            }
        }

        if (failures.Count == 0)
        {
            return await next();
        }

        return new ValidationFailed(failures.ToDictionary(kv => kv.Key, kv => (IReadOnlyList<(string, string)>)kv.Value));
    }
}
