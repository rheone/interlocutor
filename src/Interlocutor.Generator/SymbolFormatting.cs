namespace Interlocutor.Generator;

using System.Collections.Generic;
using Microsoft.CodeAnalysis;

/// <summary>
/// Shared symbol-to-string formatting used by <see cref="HandlerTypeAnalyzer"/> and
/// <see cref="GenericUsageAnalyzer"/> so both sides of the open-generic-handler match
/// (<see cref="OpenGenericHandlerModel"/> and <see cref="ClosedGenericRequestUsageModel"/>) produce identically
/// shaped strings for the same symbol regardless of which analyzer pass encountered it.
/// </summary>
internal static class SymbolFormatting
{
    private static readonly SymbolDisplayFormat QualifiedFormat =
        SymbolDisplayFormat.FullyQualifiedFormat.WithMiscellaneousOptions(SymbolDisplayMiscellaneousOptions.UseSpecialTypes);

    private static readonly SymbolDisplayFormat DefinitionKeyFormat =
        SymbolDisplayFormat.FullyQualifiedFormat.WithGenericsOptions(SymbolDisplayGenericsOptions.None);

    /// <summary>Formats a symbol as a fully-qualified, <c>global::</c>-prefixed source-safe type name.</summary>
    /// <param name="symbol">The symbol to format.</param>
    /// <returns>The formatted, source-safe type name.</returns>
    public static string Format(ITypeSymbol symbol) => symbol.ToDisplayString(QualifiedFormat);

    /// <summary>
    /// Formats a stable identity for <paramref name="symbol"/>'s open generic definition, independent of the
    /// declared names of its own type parameters - two generic types with the same name and arity but declared
    /// with differently-named type parameters produce the same key here, which is what lets an open generic
    /// handler's request-type-argument reference and a discovered closed usage elsewhere in the compilation be
    /// matched as "the same open generic type" even though each site's own type parameter names may differ.
    /// </summary>
    /// <param name="symbol">The symbol whose open generic definition to key.</param>
    /// <returns>An arity-qualified, type-parameter-name-independent identity string.</returns>
    public static string FormatDefinitionKey(ITypeSymbol symbol) =>
        $"{symbol.OriginalDefinition.ToDisplayString(DefinitionKeyFormat)}`{(symbol.OriginalDefinition as INamedTypeSymbol)?.Arity ?? 0}";

    /// <summary>
    /// Resolves <paramref name="symbol"/>'s CLR metadata name (no <c>global::</c> prefix), resolvable via
    /// <see cref="Compilation.GetTypeByMetadataName(string)"/>, or <see langword="null"/> if it is a
    /// constructed generic type, array, or other shape this generator does not attempt to resolve that way.
    /// </summary>
    /// <param name="symbol">The symbol to resolve a metadata name for.</param>
    /// <returns>The CLR metadata name, or <see langword="null"/> if unresolvable that way.</returns>
    public static string? TryGetMetadataName(ITypeSymbol symbol)
    {
        if (symbol is not INamedTypeSymbol { IsGenericType: false } named)
        {
            return null;
        }

        var parts = new List<string>();
        for (INamedTypeSymbol? current = named; current is not null; current = current.ContainingType)
        {
            parts.Insert(0, current.MetadataName);
        }

        var qualifiedName = string.Join("+", parts);
        return named.ContainingNamespace is { IsGlobalNamespace: false } ns
            ? $"{ns.ToDisplayString()}.{qualifiedName}"
            : qualifiedName;
    }
}
