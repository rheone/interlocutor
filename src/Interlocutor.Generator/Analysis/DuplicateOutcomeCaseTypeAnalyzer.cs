namespace Interlocutor.Generator.Analysis;

using System.Collections.Generic;
using System.Collections.Immutable;
using Interlocutor.Generator.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

/// <summary>
/// Reports <see cref="InterlocutorDiagnostics.DuplicateOutcomeCaseType"/> for any closed generic type
/// implementing <c>IOutcome</c> whose type argument list repeats the same type in more than one position.
/// </summary>
/// <remarks>
/// <para>
/// Unlike <see cref="HandlerDiagnosticAnalyzer"/>, which reasons about the request/handler model
/// <see cref="InterlocutorSourceGenerator"/> already built, this is a standalone <see cref="DiagnosticAnalyzer"/>:
/// the closed generic usage it looks for can appear anywhere a type is written - a request's response type
/// argument, a local variable, a field, a cast - not only at a request or handler declaration, so it is driven
/// directly by every <see cref="SyntaxKind.GenericName"/> node in the compilation rather than by the
/// generator's own request/handler discovery.
/// </para>
/// <para>
/// Deliberately not hardcoded to this library's own <c>Outcome&lt;...&gt;</c> family: any closed generic type
/// that implements <c>IOutcome</c> is checked identically, including a consumer's own custom union (see
/// <c>documentation/extensibility.md</c>'s custom <c>IOutcome</c> implementation example), since the check
/// resolves <c>IOutcome</c> membership through <see cref="ITypeSymbol.AllInterfaces"/> rather than matching a
/// type name as source text.
/// </para>
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class DuplicateOutcomeCaseTypeAnalyzer : DiagnosticAnalyzer
{
    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
    [InterlocutorDiagnostics.DuplicateOutcomeCaseType];

    /// <inheritdoc />
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterCompilationStartAction(static compilationStartContext =>
        {
            var outcomeSymbol = compilationStartContext.Compilation.GetTypeByMetadataName(WellKnownTypeNames.Outcome);
            if (outcomeSymbol is null)
            {
                // Interlocutor.CommonKernel isn't referenced by this compilation - nothing to check.
                return;
            }

            compilationStartContext.RegisterSyntaxNodeAction(
                syntaxContext => Analyze(syntaxContext, outcomeSymbol),
                SyntaxKind.GenericName
            );
        });
    }

    /// <summary>
    /// Resolves the symbol a <see cref="SyntaxKind.GenericName"/> node refers to and, if it is a closed
    /// <c>IOutcome</c> implementer, reports one diagnostic per type repeated across its type-argument list.
    /// </summary>
    /// <param name="context">The syntax node analysis context, scoped to one <see cref="GenericNameSyntax"/>.</param>
    /// <param name="outcomeSymbol">The resolved <c>IOutcome</c> marker interface symbol.</param>
    private static void Analyze(SyntaxNodeAnalysisContext context, INamedTypeSymbol outcomeSymbol)
    {
        var genericName = (GenericNameSyntax)context.Node;

        // A GenericNameSyntax is a type reference in every context it appears (return type, field, parameter,
        // base list, nested type argument, ...), so GetSymbolInfo alone resolves it; GetTypeInfo is a fallback
        // for the rare shape where symbol binding alone doesn't (e.g. inside certain expression contexts).
        var symbol =
            context.SemanticModel.GetSymbolInfo(genericName, context.CancellationToken).Symbol as INamedTypeSymbol
            ?? context.SemanticModel.GetTypeInfo(genericName, context.CancellationToken).Type as INamedTypeSymbol;

        if (symbol is not { IsGenericType: true, IsUnboundGenericType: false } || symbol.TypeArguments.Length < 2)
        {
            return;
        }

        if (!symbol.AllInterfaces.Contains(outcomeSymbol, SymbolEqualityComparer.Default))
        {
            return;
        }

        var typeArguments = symbol.TypeArguments;
        var alreadyReported = new HashSet<int>();

        for (var position = 0; position < typeArguments.Length; position++)
        {
            if (alreadyReported.Contains(position))
            {
                continue;
            }

            List<int>? duplicatePositions = null;
            for (var candidate = position + 1; candidate < typeArguments.Length; candidate++)
            {
                if (!SymbolEqualityComparer.Default.Equals(typeArguments[position], typeArguments[candidate]))
                {
                    continue;
                }

                duplicatePositions ??= [position + 1];
                duplicatePositions.Add(candidate + 1);
                alreadyReported.Add(candidate);
            }

            if (duplicatePositions is null)
            {
                continue;
            }

            context.ReportDiagnostic(
                Diagnostic.Create(
                    InterlocutorDiagnostics.DuplicateOutcomeCaseType,
                    genericName.GetLocation(),
                    symbol.ToDisplayString(),
                    typeArguments[position].ToDisplayString(),
                    string.Join(", ", duplicatePositions)
                )
            );
        }
    }
}
