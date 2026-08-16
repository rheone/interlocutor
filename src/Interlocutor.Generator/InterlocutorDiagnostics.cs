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
}
