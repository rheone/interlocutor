namespace Interlocutor.Generator;

using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

/// <summary>
/// Emits <c>Match</c>, <c>Switch</c>, per-case <c>TryGetValue</c>, <c>ToString</c>, and <c>IsSuccess</c> for
/// every <c>partial union</c> declaration attributed with <c>GenerateOutcomeMembersAttribute</c> (ticket 05/06).
/// </summary>
/// <remarks>
/// A standalone <see cref="IIncrementalGenerator"/>, separate from <see cref="InterlocutorSourceGenerator"/> -
/// it reasons purely about attributed union declarations, not about the request/handler discovery model the
/// main generator builds. Both run as ordinary generators inside the same analyzer package
/// (<c>IsRoslynComponent</c> already allows a project to contain more than one).
/// </remarks>
[Generator(LanguageNames.CSharp)]
public sealed class OutcomeMemberGenerator : IIncrementalGenerator
{
    /// <inheritdoc />
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var models = context
            .SyntaxProvider.ForAttributeWithMetadataName(
                WellKnownTypeNames.GenerateOutcomeMembersAttribute,
                static (node, _) => node is UnionDeclarationSyntax,
                static (attributeContext, cancellationToken) =>
                    OutcomeUnionModelBuilder.Build(attributeContext, cancellationToken)
            )
            .Where(static model => model is not null)
            .Select(static (model, _) => model!);

        context.RegisterSourceOutput(models, static (productionContext, model) => Execute(productionContext, model));
    }

    /// <summary>
    /// Reports <see cref="InterlocutorDiagnostics.TooFewOutcomeCaseTypes"/> for an under-arity attributed
    /// union, or otherwise emits its generated partial declaration via <see cref="OutcomeMemberEmitter"/>.
    /// </summary>
    /// <param name="context">The source production context to add the generated source (or diagnostic) to.</param>
    /// <param name="model">The attributed union to process.</param>
    private static void Execute(SourceProductionContext context, OutcomeUnionModel model)
    {
        if (model.CaseTypeDisplayStrings.Length < 2)
        {
            context.ReportDiagnostic(
                Diagnostic.Create(
                    InterlocutorDiagnostics.TooFewOutcomeCaseTypes,
                    model.Location,
                    model.DisplayName,
                    model.CaseTypeDisplayStrings.Length
                )
            );
            return;
        }

        context.AddSource(model.HintName, OutcomeMemberEmitter.Emit(model));
    }
}
