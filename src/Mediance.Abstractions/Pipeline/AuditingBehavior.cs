namespace Mediance.Abstractions.Pipeline;

using Mediance.Abstractions.Markers;
using Mediance.Abstractions.Pipeline.Classifiers;

/// <summary>Pipeline behavior that records an audit trail for requests implementing <see cref="IAuditableRequest"/>.</summary>
/// <typeparam name="TRequest">The request type.</typeparam>
/// <typeparam name="TResponse">The response type.</typeparam>
public class AuditingBehavior<TRequest, TResponse>(IAuditSink sink, IResponseOutcomeDescriber? describer = null)
    : IPipelineBehavior<TRequest, TResponse>
{
    private readonly IResponseOutcomeDescriber _describer =
        describer ?? new DefaultResponseOutcomeDescriber(OutcomeClassifierBuilder.DefaultClassifier);

    /// <summary>
    /// Handles the request, recording an audit entry if the request implements <see cref="IAuditableRequest"/>.
    /// </summary>
    /// <param name="request">The request to process.</param>
    /// <param name="next">The next behavior in the pipeline.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The result from the next behavior or handler.</returns>
    public async Task<TResponse> Handle(TRequest request, Func<Task<TResponse>> next, CancellationToken ct)
    {
        // If the request doesn't require auditing, skip this behavior entirely.
        if (request is not IAuditableRequest auditable)
        {
            return await next();
        }

        // Execute the handler first, capturing the result. This ensures we audit both success and failure outcomes.
        var result = await next();

        // Describe the outcome (e.g., "Success", "NotFound", "Forbidden") for the audit log. This gives
        // compliance/forensics a quick way to filter audits by outcome type without parsing full result objects.
        var outcome = this._describer.Describe(result);

        // Record the audit entry. This includes the action name (from IAuditableRequest.ActionName),
        // request type name (for categorization), outcome, and timestamp. The request context middleware
        // should populate UserId if available.
        var entry = new AuditEntry(auditable.ActionName, typeof(TRequest).Name, outcome, DateTime.UtcNow);
        await sink.RecordAsync(entry, ct);

        return result;
    }
}
