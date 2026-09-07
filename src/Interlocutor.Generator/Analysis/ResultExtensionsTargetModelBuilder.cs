namespace Interlocutor.Generator.Analysis;

using System.Linq;
using System.Threading;
using Interlocutor.Generator.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

/// <summary>
/// Builds a <see cref="ResultExtensionsTargetModel"/> from one <c>GenerateResultExtensionsAttribute</c>-attributed
/// syntax node found by <see cref="ResultExtensionsGenerator"/>'s <c>ForAttributeWithMetadataName</c> pipeline step.
/// </summary>
internal static class ResultExtensionsTargetModelBuilder
{
    /// <summary>
    /// Builds the model for one attributed target, or <see langword="null"/> if the attribute was found on
    /// something other than a class declaration (not a legal target per
    /// <c>GenerateResultExtensionsAttribute</c>'s <c>AttributeTargets.Class</c>, but defensive against a future
    /// language shape sharing that target).
    /// </summary>
    /// <param name="context">The attribute-target context Roslyn's incremental pipeline supplies.</param>
    /// <param name="cancellationToken">Cancels the build.</param>
    /// <returns>The built model, or <see langword="null"/> if <paramref name="context"/> isn't a class declaration.</returns>
    public static ResultExtensionsTargetModel? Build(
        GeneratorAttributeSyntaxContext context,
        CancellationToken cancellationToken
    )
    {
        _ = cancellationToken;

        if (
            context.TargetNode is not ClassDeclarationSyntax classDeclaration
            || context.TargetSymbol is not INamedTypeSymbol symbol
        )
        {
            return null;
        }

        var namespaceSymbol = symbol.ContainingNamespace;
        var namespaceName = namespaceSymbol is { IsGlobalNamespace: false } ? namespaceSymbol.ToDisplayString() : string.Empty;

        return new ResultExtensionsTargetModel(
            Namespace: namespaceName,
            ModifiersText: string.Join(" ", classDeclaration.Modifiers.Select(static m => m.Text)),
            TypeName: symbol.Name,
            HintName: $"{symbol.Name}.HttpResults.g.cs"
        );
    }
}
