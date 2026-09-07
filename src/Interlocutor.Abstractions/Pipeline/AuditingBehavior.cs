namespace Interlocutor.Abstractions.Pipeline;

using Interlocutor.Abstractions.Core;
using Interlocutor.Abstractions.Markers;
using Interlocutor.Abstractions.Pipeline.Classifiers;

/// <summary>
/// Pipeline behavior that records an audit trail for every request it runs for.
/// </summary>
/// <typeparam name="TRequest">
/// The request type. Constrained to <see cref="IAuditableRequest"/> so only requests that opt into auditing
/// close this generic - a container-neutral, compile-time-checked replacement for a runtime <c>is</c> check.
/// </typeparam>
/// <typeparam name="TResponse">The response type.</typeparam>
/// <remarks>
/// Because <typeparamref name="TRequest"/> is constrained to <see cref="IAuditableRequest"/>, both Microsoft
/// DI and Autofac skip this open generic registration entirely for a request type that does not implement the
/// marker - the container never even attempts to construct this behavior for it, so a non-auditable request
/// pays nothing for auditing being registered elsewhere in the chain.
/// </remarks>
public class AuditingBehavior<TRequest, TResponse>(
    IAuditSink sink,
    IResponseOutcomeDescriber? describer = null,
    IRequestContextAccessor? contextAccessor = null
) : IPipelineBehavior<TRequest, TResponse>
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
    public async Task<TResponse> HandleAsync(TRequest request, Func<Task<TResponse>> next, CancellationToken ct)
    {
        // Execute the handler first, capturing the result. This ensures we audit both success and failure outcomes.
        var result = await next().ConfigureAwait(false);

        // Describe the outcome (e.g., "Success", "NotFound", "Forbidden") for the audit log. This gives
        // compliance/forensics a quick way to filter audits by outcome type without parsing full result objects.
        var outcome = this._describer.Describe(result);

        // Pull the authenticated user ID from the ambient request context, if one is registered and
        // populated. Both "no accessor registered" and "accessor has no context yet" (e.g. an
        // unauthenticated request) resolve to null here rather than throwing - auditing must never fail
        // a request because identity information happens to be unavailable.
        var userId = contextAccessor?.Context?.UserId;

        // Record the audit entry. This includes the action name (from IAuditableRequest.ActionName),
        // request type name (for categorization), outcome, timestamp, and the authenticated user ID
        // (or null if anonymous or no request context is available).
        var entry = new AuditEntry(request.ActionName, typeof(TRequest).Name, outcome, DateTime.UtcNow, userId);
        await sink.RecordAsync(entry, ct).ConfigureAwait(false);

        return result;
    }
}
