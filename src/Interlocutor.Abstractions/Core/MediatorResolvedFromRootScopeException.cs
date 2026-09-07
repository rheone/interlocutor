namespace Interlocutor.Abstractions.Core;

/// <summary>
/// Thrown when <see cref="IMediator"/>/<see cref="ISender"/>/<see cref="IPublisher"/> dispatch (<c>Send</c> or
/// <c>Publish</c>) is attempted on an instance resolved directly from the root service provider/container,
/// rather than from a real scope (a per-request scope, or one explicitly created via
/// <c>IServiceScopeFactory.CreateScope()</c>/Autofac's <c>BeginLifetimeScope()</c>).
/// </summary>
/// <remarks>
/// <para>
/// The mediator interfaces are registered scoped and non-configurable; the one genuinely dangerous misuse - a
/// singleton (a background service, a hosted service, a cache) capturing the mediator at construction time -
/// resolves it from the root provider exactly once and holds onto that instance for the life of the process.
/// Every subsequent dispatch would silently run against services resolved from the root scope instead of a
/// fresh one per request: a scoped <c>DbContext</c>/<c>ISession</c> becomes process-lifetime and shared across
/// every request and thread. Development-time container scope validation may catch this; production, where
/// that validation defaults to off, does not - this exception is the backstop that fires regardless.
/// </para>
/// <para>
/// The fix is always the same regardless of which dispatch overload triggered this exception: don't hold onto
/// an <see cref="IMediator"/>/<see cref="ISender"/>/<see cref="IPublisher"/> resolved once at startup. Inject
/// <c>IServiceScopeFactory</c> (Autofac: <c>ILifetimeScope</c>) instead, and resolve the mediator fresh from a
/// scope created per unit of work - <c>using var scope = scopeFactory.CreateScope(); var mediator =
/// scope.ServiceProvider.GetRequiredService&lt;IMediator&gt;();</c>.
/// </para>
/// </remarks>
// RCS1194 wants the standard parameterless/message/message+innerException/serialization constructor set. This
// exception's RequestType is load-bearing - see HandlerNotRegisteredException's identical suppression note.
#pragma warning disable RCS1194
public sealed class MediatorResolvedFromRootScopeException : Exception
{
    /// <summary>Initializes a new instance of the <see cref="MediatorResolvedFromRootScopeException"/> class.</summary>
    /// <param name="requestType">
    /// The runtime type of the request or notification being dispatched when the root-scope guard fired.
    /// </param>
    public MediatorResolvedFromRootScopeException(Type requestType)
        : base(BuildMessage(requestType))
    {
        this.RequestType = requestType;
    }

    /// <summary>
    /// Gets the runtime type of the request or notification being dispatched when the root-scope guard fired.
    /// </summary>
    /// <value>The runtime type of the request or notification being dispatched when the root-scope guard fired.</value>
    public Type RequestType { get; }

    private static string BuildMessage(Type requestType) =>
        $"Cannot dispatch '{requestType.FullName}': the mediator was resolved from the root service "
        + "provider/container, not a real scope. This usually means a singleton (a background service, a "
        + "hosted service, a cache) captured IMediator/ISender/IPublisher at construction time. Resolve the "
        + "mediator from a scope instead - inject IServiceScopeFactory (Autofac: ILifetimeScope) and call "
        + "CreateScope()/BeginLifetimeScope() per unit of work, resolving the mediator from that scope's "
        + "provider rather than holding onto one resolved at startup.";
}
#pragma warning restore RCS1194
