namespace Interlocutor.Generator;

using System.Linq;
using Interlocutor.Generator.Analysis;
using Interlocutor.Generator.Diagnostics;
using Interlocutor.Generator.Emission;
using Interlocutor.Generator.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

/// <summary>
/// Emits <c>Match</c>, <c>Switch</c>, per-case <c>TryGetValue</c>, and <c>ToString</c> for
/// every <c>partial union</c> declaration attributed with <c>GenerateOutcomeMembersAttribute</c> (ticket 05/06).
/// </summary>
/// <remarks>
/// <para>
/// A standalone <see cref="IIncrementalGenerator"/>, separate from <see cref="InterlocutorSourceGenerator"/> -
/// it reasons purely about attributed union declarations, not about the request/handler discovery model the
/// main generator builds. Both run as ordinary generators inside the same analyzer package
/// (<c>IsRoslynComponent</c> already allows a project to contain more than one).
/// </para>
/// <para>
/// Understanding what one <c>[GenerateOutcomeMembers]</c> declaration actually produces means following it
/// through seven collaborators, each owning exactly one pipeline step, in the order the pipeline itself runs:
/// </para>
/// <list type="number">
/// <item><description>
/// <c>GenerateOutcomeMembersAttribute</c> (<c>Interlocutor.CommonKernel</c>) - the marker a consumer applies to
/// their own <c>partial union</c> declaration. It lives in the zero-dependency <c>CommonKernel</c> package, not
/// this one, so it is never itself a compile-time symbol in this project's own compilation (this project takes
/// no project reference on <c>CommonKernel</c>); <see cref="Initialize"/> below resolves it purely by its
/// fully-qualified metadata name, <see cref="WellKnownTypeNames.GenerateOutcomeMembersAttribute"/>, via Roslyn's
/// <c>ForAttributeWithMetadataName</c>, never by a direct type reference.
/// </description></item>
/// <item><description>
/// <see cref="OutcomeUnionModelBuilder"/> - <see cref="Initialize"/>'s <c>ForAttributeWithMetadataName</c> step
/// hands each attributed syntax node to <see cref="OutcomeUnionModelBuilder.Build"/>, which resolves it
/// semantically: case types, whether the union implements <c>IOutcome</c>, and each case's named accessor.
/// </description></item>
/// <item><description>
/// <see cref="OutcomeUnionModel"/> - the equatable, incremental-pipeline-safe snapshot
/// <see cref="OutcomeUnionModelBuilder.Build"/> produces and every later step consumes; nothing downstream of
/// this step touches Roslyn syntax or semantic types again.
/// </description></item>
/// <item><description>
/// <see cref="InterlocutorDiagnostics"/> - the descriptor catalog both diagnostic checks below report from:
/// <see cref="InterlocutorDiagnostics.TooFewOutcomeCaseTypes"/> for an under-arity union, and
/// <see cref="InterlocutorDiagnostics.AccessorNameCollision"/> for a colliding named accessor.
/// </description></item>
/// <item><description>
/// <see cref="OutcomeAccessorCollisionAnalyzer"/> - once <see cref="Execute"/> has confirmed the union has at
/// least two case types, this checks the resulting <see cref="OutcomeUnionModel.CaseAccessorNames"/> for a
/// collision, either between two peer case types or against a reserved generated member name, reporting
/// <see cref="InterlocutorDiagnostics.AccessorNameCollision"/> and gating emission when it finds one.
/// </description></item>
/// <item><description>
/// <see cref="OutcomeMemberEmitter"/> - once both checks pass, <see cref="Execute"/> hands the model to
/// <see cref="OutcomeMemberEmitter.Emit"/>, which renders the generated partial declaration's full source text:
/// <c>Match</c>, <c>Switch</c>, per-case <c>TryGetValue</c>, <c>ToString</c>, and equality unconditionally, plus
/// <c>ActiveCaseType</c> when the union implements <c>IOutcome</c>.
/// </description></item>
/// <item><description>
/// <see cref="OutcomeMemberGenerator"/> (this class) - the <c>[Generator]</c> entry point wiring the six
/// collaborators above together: <see cref="Initialize"/> registers the incremental pipeline that builds the
/// model, and <see cref="Execute"/> sequences the two diagnostic checks ahead of emission.
/// </description></item>
/// </list>
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
    /// union, <see cref="InterlocutorDiagnostics.AccessorNameCollision"/> for a colliding named accessor, or
    /// otherwise emits its generated partial declaration via <see cref="OutcomeMemberEmitter"/>.
    /// </summary>
    /// <param name="context">The source production context to add the generated source (or diagnostics) to.</param>
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

        // A colliding named accessor would emit an ambiguous or (for a reserved-name collision) outright
        // duplicate member - skip emission entirely rather than let the compiler's own error stand in for the
        // clearer, more actionable diagnostic already reported here.
        if (OutcomeAccessorCollisionAnalyzer.ReportAccessorNameCollisions(model, context))
        {
            return;
        }

        context.AddSource(model.HintName, OutcomeMemberEmitter.Emit(model));
    }
}
