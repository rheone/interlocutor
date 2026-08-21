namespace Interlocutor.Generator;

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

/// <summary>
/// An equatable, incremental-pipeline-safe snapshot of one concrete (fully closed) usage of some generic type
/// found anywhere in the compilation - the input <see cref="OpenGenericHandlerResolver"/> matches against every
/// discovered <see cref="OpenGenericHandlerModel"/> to find which open generic request types are actually
/// instantiated, and with which concrete type arguments.
/// </summary>
/// <remarks>
/// Discovered from every closed <c>GenericNameSyntax</c> in the compilation (see <see cref="GenericUsageAnalyzer"/>),
/// regardless of whether it happens to name an open generic request type - filtering against the handlers'
/// own <see cref="OpenGenericHandlerModel.RequestDefinitionKey"/> values happens later, in
/// <see cref="OpenGenericHandlerResolver"/>, once both sides of the match are known.
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
internal sealed record ClosedGenericRequestUsageModel(
    string RequestDefinitionKey,
    string ClosedRequestDisplayName,
    ImmutableArray<string> TypeArgumentDisplayNames,
    ImmutableArray<string?> TypeArgumentMetadataNames,
    Location Location
);
