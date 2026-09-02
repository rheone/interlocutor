namespace Interlocutor.Generator;

using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis;

/// <summary>
/// An equatable, incremental-pipeline-safe snapshot of one discovered open generic query or command handler -
/// a handler type declared with its own type parameters that closes over a family of generic requests, rather
/// than one concrete request (see <see cref="HandlerModel"/>).
/// </summary>
/// <remarks>
/// Every string here is a source-safe, <c>global::</c>-prefixed template that still names the handler's own
/// type parameters verbatim (e.g. <c>"global::MyApp.GenericQueryHandler&lt;T&gt;"</c>) - closing this handler
/// over a concrete type argument set is a whole-word substitution of each name in
/// <see cref="Constraints"/> for the bound concrete type's own display name, performed by
/// <see cref="OpenGenericHandlerResolver"/> once every discovered <see cref="ClosedGenericRequestUsageModel"/>
/// is known. Substituting by name instead of tracking symbol/position pairs keeps this model - like
/// <see cref="HandlerModel"/> - built entirely from primitive, cache-friendly types.
/// </remarks>
/// <param name="Kind">Whether this is a query or command handler.</param>
/// <param name="HandlerDefinitionKey">
/// A stable, arity-qualified identity for the handler's own open generic type, independent of its type
/// parameters' declared names - used to match a <c>HandlerOverrideAttribute</c> target against this handler.
/// </param>
/// <param name="HandlerDisplayName">The handler's open generic type as source text, e.g. <c>"global::MyApp.GenericQueryHandler&lt;T&gt;"</c>.</param>
/// <param name="RequestDefinitionKey">
/// A stable, arity-qualified identity for the open generic request type this handler targets - matched against
/// <see cref="ClosedGenericRequestUsageModel.RequestDefinitionKey"/> to find concrete instantiations to close over.
/// </param>
/// <param name="ResponseDisplayTemplate">The handler's response type as source text, still naming its own type parameters, e.g. <c>"global::MyApp.Result&lt;T&gt;"</c>.</param>
/// <param name="ServiceInterfaceDisplayTemplate">The closed handler-contract interface as source text, still naming the handler's own type parameters.</param>
/// <param name="RequestArgumentParameterNames">
/// One entry per type parameter position on the open generic request type, naming the handler's own type
/// parameter used at that position (e.g. <c>["T"]</c> for <c>GenericQuery&lt;T&gt;</c>) - the key used to bind
/// each discovered concrete request usage's own type arguments (see <see cref="ClosedGenericRequestUsageModel.TypeArgumentDisplayNames"/>)
/// to this handler's type parameters positionally.
/// </param>
/// <param name="Constraints">This handler's own type parameters' constraints, in declaration order.</param>
/// <param name="OverrideTargetKeys">
/// <see cref="HandlerDefinitionKey"/>-or-<see cref="HandlerModel.HandlerTypeName"/>-style keys named by this
/// handler's <c>HandlerOverrideAttribute</c> applications, resolved at analysis time; empty if none.
/// </param>
/// <param name="OverrideTargetDisplayNames">The display name of each entry in <see cref="OverrideTargetKeys"/>, for diagnostics.</param>
/// <param name="Location">The handler type declaration's identifier location, used to anchor diagnostics.</param>
[DebuggerDisplay("{Kind} {HandlerDisplayName}")]
internal sealed record OpenGenericHandlerModel(
    HandlerKind Kind,
    string HandlerDefinitionKey,
    string HandlerDisplayName,
    string RequestDefinitionKey,
    string ResponseDisplayTemplate,
    string ServiceInterfaceDisplayTemplate,
    ImmutableArray<string> RequestArgumentParameterNames,
    ImmutableArray<GenericParameterConstraintModel> Constraints,
    ImmutableArray<string> OverrideTargetKeys,
    ImmutableArray<string> OverrideTargetDisplayNames,
    Location Location
)
{
    /// <inheritdoc/>
    /// <remarks>
    /// Overridden for two reasons (ticket 53) - see <see cref="HandlerModel.Equals(HandlerModel?)"/>'s remarks
    /// for the full rationale, which applies here identically: <see cref="ImmutableArray{T}"/>'s own
    /// <c>Equals</c> compares its backing array by reference rather than by element, and <see cref="Location"/>
    /// is deliberately excluded since it compares the declaring <see cref="Microsoft.CodeAnalysis.SyntaxTree"/>
    /// by reference, which changes on every re-parse regardless of whether this declaration itself changed.
    /// </remarks>
    public bool Equals(OpenGenericHandlerModel? other) =>
        other is not null
        && this.Kind == other.Kind
        && this.HandlerDefinitionKey == other.HandlerDefinitionKey
        && this.HandlerDisplayName == other.HandlerDisplayName
        && this.RequestDefinitionKey == other.RequestDefinitionKey
        && this.ResponseDisplayTemplate == other.ResponseDisplayTemplate
        && this.ServiceInterfaceDisplayTemplate == other.ServiceInterfaceDisplayTemplate
        && this.RequestArgumentParameterNames.SequenceEqual(other.RequestArgumentParameterNames)
        && this.Constraints.SequenceEqual(other.Constraints)
        && this.OverrideTargetKeys.SequenceEqual(other.OverrideTargetKeys)
        && this.OverrideTargetDisplayNames.SequenceEqual(other.OverrideTargetDisplayNames);

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        // netstandard2.0 (this project's target - see ADR-0008) has no System.HashCode, so combine by hand
        // rather than pull in a polyfill package for one method. Location is deliberately excluded - see
        // the remarks on Equals above.
        var hash = 17;
        hash = (hash * 31) + this.Kind.GetHashCode();
        hash = (hash * 31) + this.HandlerDefinitionKey.GetHashCode();
        hash = (hash * 31) + this.HandlerDisplayName.GetHashCode();
        hash = (hash * 31) + this.RequestDefinitionKey.GetHashCode();
        hash = (hash * 31) + this.ResponseDisplayTemplate.GetHashCode();
        hash = (hash * 31) + this.ServiceInterfaceDisplayTemplate.GetHashCode();
        foreach (var name in this.RequestArgumentParameterNames)
        {
            hash = (hash * 31) + name.GetHashCode();
        }

        foreach (var constraint in this.Constraints)
        {
            hash = (hash * 31) + constraint.GetHashCode();
        }

        foreach (var key in this.OverrideTargetKeys)
        {
            hash = (hash * 31) + key.GetHashCode();
        }

        foreach (var name in this.OverrideTargetDisplayNames)
        {
            hash = (hash * 31) + name.GetHashCode();
        }

        return hash;
    }
}
