namespace Interlocutor.Generator.Models;

using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Interlocutor.Generator.Analysis;
using Interlocutor.Generator.Diagnostics;
using Microsoft.CodeAnalysis;

/// <summary>
/// An equatable, incremental-pipeline-safe snapshot of one concrete (fully closed) usage of some generic type
/// found anywhere in the compilation - the input <see cref="OpenGenericHandlerResolver"/> matches against every
/// discovered <see cref="OpenGenericHandlerModel"/> to find which open generic request types are actually
/// instantiated, and with which concrete type arguments.
/// </summary>
/// <remarks>
/// <para>
/// Discovered from every closed <c>GenericNameSyntax</c> in the compilation (see <see cref="GenericUsageAnalyzer"/>),
/// regardless of whether it happens to name an open generic request type - filtering against the handlers'
/// own <see cref="OpenGenericHandlerModel.RequestDefinitionKey"/> values happens later, in
/// <see cref="OpenGenericHandlerResolver"/>, once both sides of the match are known.
/// </para>
/// </remarks>
/// <param name="RequestDefinitionKey">The stable, arity-qualified identity of the generic type this usage instantiates.</param>
/// <param name="ClosedRequestDisplayName">The fully closed type as source text, e.g. <c>"global::MyApp.GenericQuery&lt;global::MyApp.Foo&gt;"</c>.</param>
/// <param name="TypeArgumentDisplayNames">Each type argument as source text, positional.</param>
/// <param name="TypeArgumentMetadataNames">
/// Each type argument's CLR metadata name (no <c>global::</c> prefix), or <see langword="null"/> for an
/// argument this generator cannot resolve that way (a constructed generic, array, or similar) - constraint
/// checking treats such an argument as satisfying every constraint rather than rejecting it, since it cannot be
/// soundly checked from a metadata name alone.
/// </param>
/// <param name="Location">Where this usage was found, used to anchor diagnostics.</param>
/// <param name="RequestKind">
/// Whether the used generic type itself implements <c>IQuery&lt;TResponse&gt;</c> or
/// <c>ICommand&lt;TResponse&gt;</c> - <see langword="null"/> for a usage of any other generic type (the common
/// case, since <see cref="GenericUsageAnalyzer"/> runs over every closed generic name in the compilation, not
/// just request types). Only a non-null usage is a candidate for
/// <see cref="InterlocutorDiagnostics.UnresolvedGenericRequestUsage"/> (ticket 15's build-time replacement for
/// the previous silent first-dispatch failure).
/// </param>
/// <param name="ResponseDisplayName">
/// The request marker's own type argument as source text (e.g. <c>TResponse</c> in
/// <c>IQuery&lt;TResponse&gt;</c>), matching <see cref="HandlerModel.ResponseTypeName"/>'s format so the two
/// can be compared directly. <see langword="null"/> whenever <see cref="RequestKind"/> is <see langword="null"/>.
/// </param>
[DebuggerDisplay("{ClosedRequestDisplayName}")]
internal sealed record ClosedGenericRequestUsageModel(
    string RequestDefinitionKey,
    string ClosedRequestDisplayName,
    ImmutableArray<string> TypeArgumentDisplayNames,
    ImmutableArray<string?> TypeArgumentMetadataNames,
    Location Location,
    RequestKind? RequestKind = null,
    string? ResponseDisplayName = null
)
{
    /// <inheritdoc/>
    /// <remarks>
    /// <para>
    /// Overridden for two reasons (ticket 53) - see <see cref="HandlerModel.Equals(HandlerModel?)"/>'s remarks
    /// for the full rationale, which applies here identically: <see cref="ImmutableArray{T}"/>'s own
    /// <c>Equals</c> compares its backing array by reference rather than by element, and <see cref="Location"/>
    /// is deliberately excluded since it compares the declaring <see cref="Microsoft.CodeAnalysis.SyntaxTree"/>
    /// by reference, which changes on every re-parse regardless of whether this usage itself changed.
    /// </para>
    /// </remarks>
    public bool Equals(ClosedGenericRequestUsageModel? other) =>
        other is not null
        && this.RequestDefinitionKey == other.RequestDefinitionKey
        && this.ClosedRequestDisplayName == other.ClosedRequestDisplayName
        && this.TypeArgumentDisplayNames.SequenceEqual(other.TypeArgumentDisplayNames)
        && this.TypeArgumentMetadataNames.SequenceEqual(other.TypeArgumentMetadataNames)
        && this.RequestKind == other.RequestKind
        && this.ResponseDisplayName == other.ResponseDisplayName;

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        // netstandard2.0 (this project's target - see ADR-0008) has no System.HashCode, so combine by hand
        // rather than pull in a polyfill package for one method. Location is deliberately excluded - see
        // the remarks on Equals above.
        var hash = 17;
        hash = (hash * 31) + this.RequestDefinitionKey.GetHashCode();
        hash = (hash * 31) + this.ClosedRequestDisplayName.GetHashCode();
        foreach (var name in this.TypeArgumentDisplayNames)
        {
            hash = (hash * 31) + name.GetHashCode();
        }

        foreach (var name in this.TypeArgumentMetadataNames)
        {
            hash = (hash * 31) + (name?.GetHashCode() ?? 0);
        }

        hash = (hash * 31) + this.RequestKind.GetHashCode();
        hash = (hash * 31) + (this.ResponseDisplayName?.GetHashCode() ?? 0);

        return hash;
    }
}
