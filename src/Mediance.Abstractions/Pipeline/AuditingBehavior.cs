namespace Mediance.Abstractions.Pipeline;

using Mediance.Abstractions.Markers;

/// <summary>Pipeline behavior that records an audit trail for requests implementing <see cref="IAuditableRequest"/>.</summary>
/// <typeparam name="TRequest">The request type.</typeparam>
/// <typeparam name="TResponse">The response type.</typeparam>
public class AuditingBehavior<TRequest, TResponse>(IAuditSink sink, IResponseOutcomeDescriber? describer = null)
    : IPipelineBehavior<TRequest, TResponse>
{
    private readonly IResponseOutcomeDescriber _describer = describer ?? DefaultResponseOutcomeDescriber.Instance;

    public async Task<TResponse> Handle(TRequest request, Func<Task<TResponse>> next, CancellationToken ct)
    {
        if (request is not IAuditableRequest auditable)
        {
            return await next();
        }

        var result = await next();
        var outcome = this._describer.Describe(result);

        var entry = new AuditEntry(auditable.ActionName, typeof(TRequest).Name, outcome, DateTime.UtcNow);
        await sink.RecordAsync(entry, ct);
        return result;
    }
}
