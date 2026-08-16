namespace Interlocutor.Abstractions.Pipeline;

using Interlocutor.Abstractions.Markers;
using Interlocutor.Abstractions.Pipeline.Classifiers;

/// <summary>
/// Pipeline behavior that records an audit trail for every request it runs for.
/// </summary>
/// <typeparam name="TRequest">
/// The request type. Constrained to <see cref="IAuditableRequest"/> so only requests that opt into auditing
/// close this generic — a container-neutral, compile-time-checked replacement for a runtime <c>is</c> check.
/// </typeparam>
/// <typeparam name="TResponse">The response type.</typeparam>
/// <remarks>
/// Because <typeparamref name="TRequest"/> is constrained to <see cref="IAuditableRequest"/>, both Microsoft
/// DI and Autofac skip this open generic registration entirely for a request type that does not implement the
/// marker — the container never even attempts to construct this behavior for it, so a non-auditable request
/// pays nothing for auditing being registered elsewhere in the chain.
/// </remarks>
public class AuditingBehavior<TRequest, TResponse>(IAuditSink sink, IResponseOutcomeDescriber? describer = null)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IAuditableRequest
{
    private readonly IResponseOutcomeDescriber _describer =
        describer ?? new DefaultResponseOutcomeDescriber(OutcomeClassifierBuilder.DefaultClassifier);

    /// <summary>
    /// Handles the request, recording an audit entry after the handler completes.
    /// </summary>
    /// <param name="request">The request to process.</param>
    /// <param name="next">The next behavior in the pipeline.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The result from the next behavior or handler.</returns>
    public async Task<TResponse> Handle(TRequest request, Func<Task<TResponse>> next, CancellationToken ct)
    {
        // Execute the handler first, capturing the result. This ensures we audit both success and failure outcomes.
        var result = await next();

        // Describe the outcome (e.g., "Success", "NotFound", "Forbidden") for the audit log. This gives
        // compliance/forensics a quick way to filter audits by outcome type without parsing full result objects.
        var outcome = this._describer.Describe(result);

        // Record the audit entry. This includes the action name (from IAuditableRequest.ActionName),
        // request type name (for categorization), outcome, and timestamp. The request context middleware
        // should populate UserId if available.
        var entry = new AuditEntry(request.ActionName, typeof(TRequest).Name, outcome, DateTime.UtcNow);
        await sink.RecordAsync(entry, ct);

        return result;
    }
}
