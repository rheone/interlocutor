namespace Interlocutor.Generator;

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

/// <summary>
/// Finds every fully closed generic type usage anywhere in the compilation - the source of concrete type
/// arguments <see cref="OpenGenericHandlerResolver"/> closes an <see cref="OpenGenericHandlerModel"/> over.
/// </summary>
/// <remarks>
/// Runs over every <see cref="GenericNameSyntax"/> in the compilation, not just ones that turn out to name an
/// open generic request type - narrowing to the request types actually declared as handler targets happens
/// later in <see cref="OpenGenericHandlerResolver"/>, once every <see cref="OpenGenericHandlerModel"/> is also
/// known. A generic name still referencing an enclosing type parameter (for example <c>GenericQuery&lt;T&gt;</c>
/// inside the open request or handler's own declaration) is not a usage in this sense and is excluded.
/// </remarks>
internal static class GenericUsageAnalyzer
{
    /// <summary>Fast syntactic filter: any generic name syntax is a candidate.</summary>
    /// <param name="node">The syntax node to test.</param>
    /// <returns><see langword="true"/> if <paramref name="node"/> is a <see cref="GenericNameSyntax"/>.</returns>
    public static bool IsCandidate(SyntaxNode node) => node is GenericNameSyntax;

    /// <summary>Runs full semantic analysis on one syntactically-qualifying candidate.</summary>
    /// <param name="context">The generator syntax context for the candidate node.</param>
    /// <returns>The discovered closed usage, or <see langword="null"/> if the node does not name a fully closed generic type.</returns>
    public static ClosedGenericRequestUsageModel? Analyze(GeneratorSyntaxContext context)
    {
        var genericName = (GenericNameSyntax)context.Node;
        if (
            context.SemanticModel.GetSymbolInfo(genericName).Symbol is not INamedTypeSymbol { IsGenericType: true } namedType
            || ContainsOpenTypeParameter(namedType)
        )
        {
            return null;
        }

        var (requestKind, responseDisplayName) = ClassifyRequestKind(namedType, context.SemanticModel.Compilation);

        return new ClosedGenericRequestUsageModel(
            SymbolFormatting.FormatDefinitionKey(namedType),
            SymbolFormatting.Format(namedType),
            namedType.TypeArguments.Select(SymbolFormatting.Format).ToImmutableArray(),
            namedType.TypeArguments.Select(SymbolFormatting.TryGetMetadataName).ToImmutableArray(),
            genericName.GetLocation(),
            requestKind,
            responseDisplayName
        );
    }

    /// <summary>
    /// Whether <paramref name="namedType"/> itself implements <c>IQuery&lt;TResponse&gt;</c> or
    /// <c>ICommand&lt;TResponse&gt;</c> - recognized here regardless of which assembly declares
    /// <paramref name="namedType"/>, since the semantic model resolves symbols across the whole reference
    /// graph even though open generic handler closing itself only spans the current compilation's own syntax
    /// (see <see cref="OpenGenericHandlerResolver"/>'s remarks and <see cref="InterlocutorDiagnostics.UnresolvedGenericRequestUsage"/>).
    /// </summary>
    /// <param name="namedType">The closed generic type usage to classify.</param>
    /// <param name="compilation">The compilation to resolve the <c>IQuery&lt;&gt;</c>/<c>ICommand&lt;&gt;</c> marker types from.</param>
    /// <returns>
    /// The request kind and its marker's own type argument as source text, or <c>(null, null)</c> when
    /// <paramref name="namedType"/> implements neither marker.
    /// </returns>
    private static (RequestKind? Kind, string? ResponseDisplayName) ClassifyRequestKind(
        INamedTypeSymbol namedType,
        Compilation compilation
    )
    {
        var queryMarker = compilation.GetTypeByMetadataName(WellKnownTypeNames.Query);
        var commandMarker = compilation.GetTypeByMetadataName(WellKnownTypeNames.Command);

        foreach (var candidateInterface in namedType.AllInterfaces)
        {
            if (!candidateInterface.IsGenericType)
            {
                continue;
            }

            if (SymbolEqualityComparer.Default.Equals(candidateInterface.OriginalDefinition, queryMarker))
            {
                return (RequestKind.Query, SymbolFormatting.Format(candidateInterface.TypeArguments[0]));
            }

            if (SymbolEqualityComparer.Default.Equals(candidateInterface.OriginalDefinition, commandMarker))
            {
                return (RequestKind.Command, SymbolFormatting.Format(candidateInterface.TypeArguments[0]));
            }
        }

        return (null, null);
    }

    /// <summary>Whether <paramref name="type"/> still refers to an open type parameter anywhere in its structure.</summary>
    private static bool ContainsOpenTypeParameter(ITypeSymbol type) =>
        type switch
        {
            ITypeParameterSymbol => true,
            INamedTypeSymbol { IsGenericType: true } named => named.TypeArguments.Any(ContainsOpenTypeParameter),
            IArrayTypeSymbol array => ContainsOpenTypeParameter(array.ElementType),
            _ => false,
        };
}
