using FluentValidation;
using Mediance.Abstractions;
using Mediance.Abstractions.Pipeline;
using Mediance.CommonKernel;

namespace Mediance.FluentValidation;

public sealed class ValidationBehavior<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
{
    public async Task<Result<TResponse>> Handle(TRequest request, Func<Task<Result<TResponse>>> next, CancellationToken ct)
    {
        if (!validators.Any())
        {
            return await next();
        }

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

        return new Result<TResponse>(
            new ValidationFailed(failures.ToDictionary(kv => kv.Key, kv => (IReadOnlyList<(string, string)>)kv.Value))
        );
    }
}
