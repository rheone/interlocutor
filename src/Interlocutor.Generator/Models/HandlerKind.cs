namespace Interlocutor.Generator.Models;

using Interlocutor.Generator.Analysis;

/// <summary>
/// Discriminates which of the four mediator handler contracts a discovered handler implements.
/// </summary>
/// <remarks>
/// Mirrors the four contracts <c>Interlocutor.DependencyInjection.Abstractions.HandlerDiscovery</c> checks
/// at runtime - this generator replicates that same discovery semantic at compile time.
/// </remarks>
/// <remarks>
/// This enum's underlying <see langword="int"/> values are encoded into assembly-level metadata
/// (<c>InterlocutorGeneratedHandlerAttribute</c>) by one generator run and decoded by a later run reading a
/// referenced assembly (see <see cref="ReferencedModuleDiscovery"/>) - potentially a different generator
/// version on each side. Decoding validates membership with <see cref="Enum.IsDefined(System.Type,object)"/>
/// rather than assuming <see cref="Notification"/> is the highest value, so member order here is not
/// load-bearing for that path - but removing or renumbering an existing case is still a breaking change for
/// any already-compiled referenced assembly's encoded values.
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
