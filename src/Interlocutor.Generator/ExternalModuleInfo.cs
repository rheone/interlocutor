namespace Interlocutor.Generator;

using System.Collections.Immutable;

/// <summary>
/// An equatable, incremental-pipeline-safe snapshot of one referenced assembly's compile-time generated
/// registration module (ticket 12), discovered by <see cref="ReferencedModuleDiscovery"/> via
/// <c>InterlocutorGeneratedModuleAttribute</c> assembly metadata alone.
/// </summary>
/// <param name="AssemblyIdentity">
/// The declaring assembly's identity string (<c>IAssemblySymbol.Identity.ToString()</c>) - the dedup key a
/// diamond-shaped reference graph (the same assembly reachable via more than one path) collapses on, since
/// two different reference paths to the same assembly must never produce two aggregated copies of its
/// registrations.
/// </param>
/// <param name="QualifiedModuleTypeName">
/// The module type's fully-qualified (<c>global::</c>-prefixed) name, exactly as the referenced assembly's
/// own generator run emitted it - used verbatim as the source-level expression the aggregating module's
/// <c>Registrations</c> property concatenates.
/// </param>
/// <param name="Handlers">
/// Every query, command, or void-command handler the referenced assembly declared, restated from its
/// <c>InterlocutorGeneratedHandlerAttribute</c> metadata for the missing-handler diagnostic.
/// </param>
internal sealed record ExternalModuleInfo(
    string AssemblyIdentity,
    string QualifiedModuleTypeName,
    ImmutableArray<ExternalHandlerInfo> Handlers
);
