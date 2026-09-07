namespace Interlocutor.Generator.Models;

using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Interlocutor.Generator.Analysis;

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
/// Every query, command, or bare-command handler the referenced assembly declared, restated from its
/// <c>InterlocutorGeneratedHandlerAttribute</c> metadata for the missing-handler diagnostic.
/// </param>
/// <remarks>
/// <see cref="ImmutableArray{T}"/>'s own <c>Equals</c> compares the underlying array by reference, not by
/// element - so the compiler-generated positional-record equality this type would otherwise get from
/// <paramref name="Handlers"/> is useless for incremental-pipeline caching (two separately-built arrays with
/// identical contents never compare equal). <see cref="Equals(ExternalModuleInfo?)"/>/<see cref="GetHashCode"/>
/// are overridden below to compare <see cref="Handlers"/> element-wise instead.
/// </remarks>
[DebuggerDisplay("{AssemblyIdentity} ({Handlers.Length} handlers)")]
internal sealed record ExternalModuleInfo(
    string AssemblyIdentity,
    string QualifiedModuleTypeName,
    ImmutableArray<ExternalHandlerInfo> Handlers
)
{
    /// <inheritdoc/>
    public bool Equals(ExternalModuleInfo? other) =>
        other is not null
        && this.AssemblyIdentity == other.AssemblyIdentity
        && this.QualifiedModuleTypeName == other.QualifiedModuleTypeName
        && this.Handlers.SequenceEqual(other.Handlers);

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        // netstandard2.0 (this project's target - see ADR-0008) has no System.HashCode, so combine by hand
        // rather than pull in a polyfill package for one method.
        var hash = 17;
        hash = (hash * 31) + this.AssemblyIdentity.GetHashCode();
        hash = (hash * 31) + this.QualifiedModuleTypeName.GetHashCode();
        foreach (var handler in this.Handlers)
        {
            hash = (hash * 31) + handler.GetHashCode();
        }

        return hash;
    }
}
