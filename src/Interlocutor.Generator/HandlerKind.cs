namespace Interlocutor.Generator;

/// <summary>
/// Discriminates which of the four mediator handler contracts a discovered handler implements.
/// </summary>
/// <remarks>
/// Mirrors the four contracts <c>Interlocutor.DependencyInjection.Abstractions.HandlerDiscovery</c> checks
/// at runtime - this generator replicates that same discovery semantic at compile time.
/// </remarks>
internal enum HandlerKind
{
    /// <summary>An <c>IQueryHandler&lt;TQuery, TResponse&gt;</c> implementation.</summary>
    Query,

    /// <summary>An <c>ICommandHandler&lt;TCommand, TResponse&gt;</c> implementation.</summary>
    Command,

    /// <summary>An <c>IVoidCommandHandler&lt;TCommand, TResult&gt;</c> implementation.</summary>
    VoidCommand,

    /// <summary>An <c>INotificationHandler&lt;TNotification&gt;</c> implementation.</summary>
    Notification,
}
