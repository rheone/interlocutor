using FluentValidation;
using Mediance.Abstractions;
using Mediance.Abstractions.Commands;
using Mediance.Abstractions.Pipeline;
using Mediance.CommonKernel;

namespace Mediance.FluentValidation;

public sealed class BulkItemValidationBehavior<TRequest, TItem, TResponseItem>(IValidator<TItem> itemValidator)
    : IPipelineBehavior<TRequest, IReadOnlyList<TResponseItem>>
    where TRequest : IBulkCommand<TItem, TResponseItem>
{
    public async Task<Result<IReadOnlyList<TResponseItem>>> Handle(
        TRequest request,
        Func<Task<Result<IReadOnlyList<TResponseItem>>>> next,
        CancellationToken ct
    )
    {
        var failures = new Dictionary<string, List<(string Code, string Message)>>();

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

        return new Result<IReadOnlyList<TResponseItem>>(
            new ValidationFailed(failures.ToDictionary(kv => kv.Key, kv => (IReadOnlyList<(string, string)>)kv.Value))
        );
    }
}
