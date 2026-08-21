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
    /// A handler for this request living in a different, referenced assembly is considered too (ticket 12) -
    /// see <see cref="ReferencedModuleDiscovery"/> - as long as that assembly's own generated module is reachable
    /// through the current compilation's reference graph, transitively. Only a handler declared purely in a
    /// sibling assembly the current compilation neither directly nor transitively references, or one registered
    /// only via runtime assembly scanning, is invisible to this check.
    /// </remarks>
    public static readonly DiagnosticDescriptor MissingHandler = new(
        id: "INTERLOC001",
        title: "No handler registered for request type",
        messageFormat: "Request type '{0}' has no matching handler in this compilation",
        category: "Interlocutor.Discovery",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "A type implementing IQuery<TResponse>, ICommand<TResponse>, or ICommand was found with no "
            + "corresponding IQueryHandler<,>, ICommandHandler<,>, or IVoidCommandHandler<,> implementation "
            + "reachable from this compilation, including transitively-referenced assemblies. Add a handler, or "
            + "reference the assembly that defines one."
    );
}
