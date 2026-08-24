namespace Interlocutor.DependencyInjection.Abstractions;

/// <summary>
/// Discriminates which of the mediator handler contracts a compile-time discovered handler implements,
/// as carried by <see cref="InterlocutorGeneratedHandlerAttribute"/> for cross-assembly discovery.
/// </summary>
/// <remarks>
/// Mirrors <c>Interlocutor.Generator.HandlerKind</c> (internal to the generator) member-for-member and in
/// the same declaration order, so the generator can cast a raw attribute-argument <see langword="int"/> read
/// back from another assembly's metadata straight into its own internal enum without a translation table.
/// </remarks>
public enum GeneratedHandlerKind
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
