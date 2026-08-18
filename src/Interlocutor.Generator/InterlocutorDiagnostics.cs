namespace Interlocutor.Generator;

using Microsoft.CodeAnalysis;

/// <summary>
/// The diagnostic descriptors <see cref="InterlocutorSourceGenerator"/> reports.
/// </summary>
internal static class InterlocutorDiagnostics
{
    /// <summary>
    /// Reported when a type implements <c>IQuery&lt;TResponse&gt;</c>, <c>ICommand&lt;TResponse&gt;</c>, or
    /// <c>ICommand</c> but the compiling project defines no matching handler for it.
    /// </summary>
    /// <remarks>
    /// Default severity is <see cref="DiagnosticSeverity.Warning"/> - per ADR-0008 and this ticket's acceptance
    /// criteria, a consumer escalates it to an error via the standard <c>.editorconfig</c> mechanism every other
    /// diagnostic in this repository uses:
    /// <code>
    /// dotnet_diagnostic.INTERLOC001.severity = error
    /// </code>
    /// Cross-assembly discovery (a handler for this request living in a different, referenced assembly) is out
    /// of scope for this generator - see ticket 12 - so this diagnostic only ever considers handlers declared in
    /// the current compilation.
    /// </remarks>
    public static readonly DiagnosticDescriptor MissingHandler = new(
        id: "INTERLOC001",
        title: "No handler registered for request type",
        messageFormat: "Request type '{0}' has no matching handler in this compilation",
        category: "Interlocutor.Discovery",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "A type implementing IQuery<TResponse>, ICommand<TResponse>, or ICommand was found with no "
            + "corresponding IQueryHandler<,>, ICommandHandler<,>, or IVoidCommandHandler<,> implementation in "
            + "the same compilation. Add a handler, or reference the assembly that defines one (cross-assembly "
            + "aggregation is not yet performed by this generator)."
    );

    /// <summary>
    /// Reported when more than one concrete <c>IQueryHandler&lt;TQuery, TResponse&gt;</c> or
    /// <c>ICommandHandler&lt;TCommand, TResponse&gt;</c> implementation is found in the compiling project for the
    /// same closed request/response pair.
    /// </summary>
    /// <remarks>
    /// Unlike a notification (see <see cref="RequestKind"/>'s remarks), a query or command must resolve to
    /// exactly one handler - <c>Mediator.Send</c> and the generated dispatch wrapper both resolve the handler's
    /// service interface as a single service, so a second registration silently shadows the first rather than
    /// being an error at resolution time. This diagnostic catches that ambiguity at build time instead. Default
    /// severity is <see cref="DiagnosticSeverity.Warning"/>, escalated the same way as
    /// <see cref="MissingHandler"/>:
    /// <code>
    /// dotnet_diagnostic.INTERLOC002.severity = error
    /// </code>
    /// </remarks>
    public static readonly DiagnosticDescriptor DuplicateHandler = new(
        id: "INTERLOC002",
        title: "Multiple handlers registered for request type",
        messageFormat: "Request type '{0}' has multiple matching handlers in this compilation: {1}",
        category: "Interlocutor.Discovery",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "More than one IQueryHandler<,> or ICommandHandler<,> implementation was found for the same "
            + "closed request/response pair in the same compilation. A query or command must resolve to exactly "
            + "one handler, unlike a notification, which permits zero or many. Remove or rename the extra "
            + "handler so only one remains."
    );
}
