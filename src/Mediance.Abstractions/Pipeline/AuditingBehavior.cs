using Mediance.Abstractions.Markers;
using Mediance.CommonKernel;

namespace Mediance.Abstractions.Pipeline;

/// <summary>Pipeline behavior that records an audit trail for requests implementing <see cref="IAuditableRequest"/>.</summary>
public class AuditingBehavior<TRequest, TResponse>(IAuditSink sink) : IPipelineBehavior<TRequest, TResponse>
{
    public async Task<Result<TResponse>> Handle(TRequest request, Func<Task<Result<TResponse>>> next, CancellationToken ct)
    {
        if (request is not IAuditableRequest auditable)
        {
            return await next();
        }

        var result = await next();
        var outcome = result.Value switch
        {
            TResponse => "Success",
            ValidationFailed => "ValidationFailed",
            RuleViolation => "RuleViolation",
            NotFound => "NotFound",
            Forbidden => "Forbidden",
            Conflict => "Conflict",
            _ => "Unknown",
        };

        var entry = new AuditEntry(auditable.ActionName, typeof(TRequest).Name, outcome, DateTime.UtcNow);
        await sink.RecordAsync(entry, ct);
        return result;
    }
}
