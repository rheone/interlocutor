namespace Interlocutor.Generator;

using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

/// <summary>
/// Builds an <see cref="OutcomeUnionModel"/> from one <c>GenerateOutcomeMembersAttribute</c>-attributed
/// syntax node found by <see cref="OutcomeMemberGenerator"/>'s <c>ForAttributeWithMetadataName</c> pipeline step.
/// </summary>
internal static class OutcomeUnionModelBuilder
{
    /// <summary>
    /// Builds the model for one attributed target, or <see langword="null"/> if the attribute was found on
    /// something other than a <c>union</c> declaration (not a legal target per <c>GenerateOutcomeMembersAttribute</c>'s
    /// <c>AttributeTargets.Struct</c>, but defensive against a future language shape sharing that target).
    /// </summary>
    /// <param name="context">The attribute-target context Roslyn's incremental pipeline supplies.</param>
    /// <param name="cancellationToken">Cancels the build.</param>
    /// <returns>The built model, or <see langword="null"/> if <paramref name="context"/> isn't a union declaration.</returns>
    public static OutcomeUnionModel? Build(GeneratorAttributeSyntaxContext context, CancellationToken cancellationToken)
    {
        if (
            context.TargetNode is not UnionDeclarationSyntax unionDeclaration
            || context.TargetSymbol is not INamedTypeSymbol symbol
        )
        {
            return null;
        }

        var caseTypeDisplayStrings = (unionDeclaration.ParameterList?.Parameters ?? default)
            .Where(static parameter => parameter.Type is not null)
            .Select(parameter =>
            {
                var caseType = context.SemanticModel.GetTypeInfo(parameter.Type!, cancellationToken).Type;

                // Falling back to the parameter's own source text only matters if the type genuinely failed to
                // bind (a compile error elsewhere) - in every normal case this resolves to a real symbol.
                return caseType?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) ?? parameter.Type!.ToString();
            })
            .ToImmutableArray();

        var namespaceSymbol = symbol.ContainingNamespace;
        var namespaceName = namespaceSymbol is { IsGlobalNamespace: false } ? namespaceSymbol.ToDisplayString() : string.Empty;

        return new OutcomeUnionModel(
            Namespace: namespaceName,
            ModifiersText: string.Join(" ", unionDeclaration.Modifiers.Select(static m => m.Text)),
            TypeName: symbol.Name,
            TypeParameterNames: symbol.TypeParameters.Select(static tp => tp.Name).ToImmutableArray(),
            CaseTypeDisplayStrings: caseTypeDisplayStrings,
            DisplayName: symbol.ToDisplayString(),
            HintName: BuildHintName(symbol, namespaceName),
            Location: unionDeclaration.Identifier.GetLocation()
        );
    }

    /// <summary>Builds a unique, filesystem-safe <c>AddSource</c> hint name for <paramref name="symbol"/>.</summary>
    /// <param name="symbol">The attributed union's symbol.</param>
    /// <param name="namespaceName">The union's containing namespace, already resolved.</param>
    /// <returns>A hint name unique across every attributed union in the compilation.</returns>
    private static string BuildHintName(INamedTypeSymbol symbol, string namespaceName)
    {
        var namespaceSegment = namespaceName.Length == 0 ? string.Empty : IdentifierSanitizer.ToIdentifier(namespaceName) + ".";

        // MetadataName (e.g. "Outcome`2") already disambiguates arity, so two differently-closed-arity
        // declarations sharing a simple name never collide.
        return $"{namespaceSegment}{IdentifierSanitizer.ToIdentifier(symbol.MetadataName)}.OutcomeMembers.g.cs";
    }
}
