namespace Interlocutor.Generator.Models;

using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Interlocutor.Generator.Analysis;
using Interlocutor.Generator.Models;

/// <summary>
/// An equatable, incremental-pipeline-safe snapshot of everything <see cref="GenericConstraintChecker"/> needs
/// to decide whether a concrete generic type argument satisfies an open generic handler's own type parameter
/// constraints (ticket 11) - without holding onto the live <see cref="Microsoft.CodeAnalysis.Compilation"/>
/// itself (ticket 53).
/// </summary>
/// <param name="ArgumentTypeFacts">
/// Value-type/reference-type/parameterless-constructor facts for every distinct concrete type argument's CLR
/// metadata name found anywhere in the compilation's closed generic usages - see
/// <see cref="ClosedGenericRequestUsageModel.TypeArgumentMetadataNames"/>. An argument metadata name absent
/// here means <see cref="Microsoft.CodeAnalysis.Compilation.GetTypeByMetadataName(string)"/> could not resolve
/// it, mirroring <see cref="GenericConstraintChecker.Satisfies"/>'s original "unresolvable - treat as
/// satisfied" fallback.
/// </param>
/// <param name="ConversionFacts">
/// Whether a discovered conversion from a concrete argument type to an open generic handler's constraint type
/// is an identity or implicit conversion (the only conversions <see cref="GenericConstraintChecker.Satisfies"/>
/// accepts), for every distinct (argument, constraint type) metadata name pair the compilation's open generic
/// handlers and closed usages could combine into. A pair absent here means one side could not be resolved,
/// again mirroring the original "unresolvable - treat as satisfied" fallback.
/// </param>
/// <remarks>
/// Built once per compilation by <see cref="GenericConstraintSnapshotBuilder.Build"/> from
/// <see cref="Microsoft.CodeAnalysis.IncrementalGeneratorInitializationContext.CompilationProvider"/> combined
/// with the already-collected open generic handler and generic usage arrays (see
/// <see cref="InterlocutorSourceGenerator.Initialize"/>'s remarks) -
/// that producing step still re-runs on every rebuild, since <see cref="Microsoft.CodeAnalysis.Compilation"/>
/// is never equal to itself across runs, but this record's own structural equality is what lets everything
/// downstream in the incremental pipeline skip recomputing when the *facts* it captured did not change.
/// <see cref="ImmutableArray{T}"/>'s own <c>Equals</c> compares its backing array by reference, not by
/// element, so - exactly as <see cref="CompilationInfo"/> and <see cref="ExternalModuleInfo"/> already do for
/// ticket 12 - <see cref="Equals(GenericConstraintSnapshot?)"/>/<see cref="GetHashCode"/> are overridden below
/// to compare both arrays element-wise instead.
/// </remarks>
[DebuggerDisplay("{ArgumentTypeFacts.Length} argument facts, {ConversionFacts.Length} conversion facts")]
internal sealed record GenericConstraintSnapshot(
    ImmutableArray<ResolvedArgumentTypeFact> ArgumentTypeFacts,
    ImmutableArray<ConstraintConversionFact> ConversionFacts
)
{
    /// <inheritdoc/>
    public bool Equals(GenericConstraintSnapshot? other) =>
        other is not null
        && this.ArgumentTypeFacts.SequenceEqual(other.ArgumentTypeFacts)
        && this.ConversionFacts.SequenceEqual(other.ConversionFacts);

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        // netstandard2.0 (this project's target - see ADR-0008) has no System.HashCode, so combine by hand
        // rather than pull in a polyfill package for one method.
        var hash = 17;
        foreach (var fact in this.ArgumentTypeFacts)
        {
            hash = (hash * 31) + fact.GetHashCode();
        }

        foreach (var fact in this.ConversionFacts)
        {
            hash = (hash * 31) + fact.GetHashCode();
        }

        return hash;
    }
}
