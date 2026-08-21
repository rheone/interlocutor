namespace Interlocutor.Generator;

using System.Collections.Immutable;
using System.Linq;

/// <summary>
/// An equatable, incremental-pipeline-safe snapshot of everything <see cref="InterlocutorSourceGenerator"/>
/// needs from the compilation itself, rather than from an individual candidate type declaration: the
/// namespace segment this compilation's own generated module is emitted under, and every upstream module
/// discovered across the reference graph (ticket 12).
/// </summary>
/// <param name="AssemblyNamespaceSegment">
/// This compilation's own sanitized assembly-name namespace segment - see
/// <see cref="IdentifierSanitizer.ToNamespaceSegment(string)"/>.
/// </param>
/// <param name="ExternalModules">Every referenced assembly's generated registration module, if any.</param>
/// <remarks>
/// Structural equality on <see cref="ExternalModules"/> is what lets this pipeline step itself be cached when a
/// rebuild's reference graph is unchanged (a re-run whose <see cref="CompilationInfo"/> compares equal to the
/// last one is reused rather than re-computed) - see <see cref="ExternalModuleInfo"/>'s own remarks for why the
/// compiler-generated positional-record equality this type would otherwise get is not enough on its own:
/// <see cref="ImmutableArray{T}"/> compares its backing array by reference, not by element, so
/// <see cref="Equals(CompilationInfo?)"/>/<see cref="GetHashCode"/> are overridden below to compare
/// <see cref="ExternalModules"/> element-wise instead. See <see cref="InterlocutorSourceGenerator.Initialize"/>'s
/// remarks for why this step being cached does not, on its own, make the whole generator skip re-emission.
/// </remarks>
internal sealed record CompilationInfo(string AssemblyNamespaceSegment, ImmutableArray<ExternalModuleInfo> ExternalModules)
{
    /// <inheritdoc/>
    public bool Equals(CompilationInfo? other) =>
        other is not null
        && this.AssemblyNamespaceSegment == other.AssemblyNamespaceSegment
        && this.ExternalModules.SequenceEqual(other.ExternalModules);

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        // netstandard2.0 (this project's target - see ADR-0008) has no System.HashCode, so combine by hand
        // rather than pull in a polyfill package for one method.
        var hash = 17;
        hash = (hash * 31) + this.AssemblyNamespaceSegment.GetHashCode();
        foreach (var module in this.ExternalModules)
        {
            hash = (hash * 31) + module.GetHashCode();
        }

        return hash;
    }
}
