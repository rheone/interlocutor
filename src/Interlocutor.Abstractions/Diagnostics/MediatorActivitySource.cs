namespace Interlocutor.Abstractions.Diagnostics;

using System.Diagnostics;

/// <summary>
/// Hosts the single <see cref="ActivitySource"/> the mediator uses to emit distributed-tracing spans for every
/// dispatch, independent of whether <see cref="Pipeline.LoggingBehavior{TRequest,TResponse}"/> is also registered.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="Mediator"/> starts one <see cref="Activity"/> per <c>Send</c>/<c>Publish</c> call via
/// <see cref="Source"/>. When nothing is listening to <see cref="Name"/> (the common case in production without an
/// attached exporter), <see cref="ActivitySource.StartActivity(string, ActivityKind)"/> returns <see langword="null"/>,
/// and every subsequent tagging call in the mediator is guarded on that null reference, so the dispatch path performs
/// no allocation and no string formatting when unobserved. That short-circuit is the standard
/// <see cref="System.Diagnostics.Activity"/> mechanism's built-in behavior - Interlocutor relies on it rather than
/// re-implementing it.
/// </para>
/// <para>
/// This mechanism is independent of <see cref="Pipeline.LoggingBehavior{TRequest,TResponse}"/>: that behavior is an
/// opt-in pipeline stage writing to <c>ILogger</c>, while this one is built into <see cref="Mediator"/> itself and
/// emits <see cref="Activity"/> spans. Neither shares state with, nor depends on, the other - both may be active,
/// neither, or either alone, with no conflict.
/// </para>
/// <para>
/// Attach an <see cref="ActivityListener"/> whose <see cref="ActivityListener.ShouldListenTo"/> matches
/// <see cref="Name"/> (or, with OpenTelemetry, call <c>TracerProviderBuilder.AddSource(MediatorActivitySource.Name)</c>)
/// to observe mediator dispatch spans.
/// </para>
/// </remarks>
public static class MediatorActivitySource
{
    /// <summary>
    /// The stable, documented name of the mediator's <see cref="ActivitySource"/>. Register this name with a
    /// listener (an <see cref="ActivityListener"/>, or an OpenTelemetry <c>AddSource</c> call) to observe mediator
    /// dispatch spans.
    /// </summary>
    public const string Name = "Interlocutor.Mediator";

    /// <summary>The tag key carrying the runtime type of the dispatched request or notification.</summary>
    public const string RequestTypeTag = "interlocutor.request_type";

    /// <summary>The tag key carrying the runtime type(s) of the handler(s) resolved to serve the request.</summary>
    public const string HandlerTypeTag = "interlocutor.handler_type";

    /// <summary>The tag key carrying the human-readable label of the resulting <c>OperationOutcome</c> case.</summary>
    public const string OutcomeTag = "interlocutor.outcome";

    /// <summary>
    /// The single <see cref="ActivitySource"/> instance every mediator dispatch starts activities from. Internal -
    /// external code observes it through a listener registered against <see cref="Name"/>, not through this field.
    /// </summary>
    internal static readonly ActivitySource Source = new(Name);
}
