namespace Interlocutor.Generator;

using System.Collections.Immutable;
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
);
