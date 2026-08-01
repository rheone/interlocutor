using Mediance.Abstractions.Markers;

namespace Mediance.Abstractions.Pipeline;

/// <summary>Pipeline behavior that records an audit trail for requests implementing <see cref="IAuditableRequest"/>.</summary>
public class AuditingBehavior<TRequest, TResponse>(IAuditSink sink, IResponseOutcomeDescriber? describer = null)
    : IPipelineBehavior<TRequest, TResponse>
{
    private readonly IResponseOutcomeDescriber describer = describer ?? DefaultResponseOutcomeDescriber.Instance;

    public async Task<TResponse> Handle(TRequest request, Func<Task<TResponse>> next, CancellationToken ct)
    {
        if (request is not IAuditableRequest auditable)
        {
            return await next();
        }

        var result = await next();
        var outcome = this.describer.Describe(result);

        var entry = new AuditEntry(auditable.ActionName, typeof(TRequest).Name, outcome, DateTime.UtcNow);
        await sink.RecordAsync(entry, ct);
        return result;
    }
}
