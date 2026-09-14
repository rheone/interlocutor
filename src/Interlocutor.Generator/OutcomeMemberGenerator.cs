namespace Interlocutor.Generator;

using System.Globalization;
using System.Linq;
using Interlocutor.Generator.Analysis;
using Interlocutor.Generator.Diagnostics;
using Interlocutor.Generator.Emission;
using Interlocutor.Generator.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

/// <summary>
/// Emits a per-case static <c>Create</c> factory, <c>Match</c>, <c>Switch</c>, per-case <c>TryGetValue</c>,
/// <c>ToString</c>, and value-based equality for every <c>partial union</c> declaration attributed with
/// <c>GenerateOutcomeMembersAttribute</c> (ticket 05/06) - plus <c>IOutcomeCase</c>/<c>ActiveCaseType</c> when
/// the declaration also lists <c>IOutcome</c> in its base list. Every case-indexed member is keyed by the case
/// types' plain declaration order in the attributed union's own type parameter list; that ordinal position is
/// bookkeeping only - see <see cref="OutcomeUnionModel.CaseTypeDisplayStrings"/>'s remarks.
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
/// <see cref="InterlocutorDiagnostics"/> - the descriptor catalog all four diagnostic checks below report
/// from: <see cref="InterlocutorDiagnostics.TooFewOutcomeCaseTypes"/> for an under-arity union,
/// <see cref="InterlocutorDiagnostics.TooManyOutcomeCaseTypes"/> for an over-arity one,
/// <see cref="InterlocutorDiagnostics.RecommendedOutcomeArityExceeded"/> for one past the configured
/// recommended maximum (non-blocking), and <see cref="InterlocutorDiagnostics.AccessorNameCollision"/> for a
/// colliding named accessor.
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
/// a per-case static <c>Create</c> factory, <c>Match</c>, <c>Switch</c>, per-case <c>TryGetValue</c>,
/// <c>ToString</c>, and equality unconditionally, plus <c>IOutcomeCase</c> and <c>ActiveCaseType</c> when the
/// union implements <c>IOutcome</c>.
/// </description></item>
/// <item><description>
/// <see cref="OutcomeMemberGenerator"/> (this class) - the <c>[Generator]</c> entry point wiring the six
/// collaborators above together: <see cref="Initialize"/> registers the incremental pipeline that builds the
/// model, and <see cref="Execute"/> sequences the four diagnostic checks ahead of emission.
/// </description></item>
/// </list>
/// </remarks>
[Generator(LanguageNames.CSharp)]
public sealed class OutcomeMemberGenerator : IIncrementalGenerator
{
    /// <summary>
    /// The most case types <c>Match&lt;TResult&gt;</c>/<c>Switch</c> can bind, one delegate parameter per case,
    /// to the BCL's widest <c>System.Func&lt;T1..T16, TResult&gt;</c>/<c>System.Action&lt;T1..T16&gt;</c>
    /// overload.
    /// </summary>
    private const int MaxCaseTypes = 16;

    /// <summary>
    /// The most case types an attributed union can declare before <see cref="InterlocutorDiagnostics.RecommendedOutcomeArityExceeded"/>
    /// fires, absent an <c>interlocutor_max_recommended_outcome_arity</c> <c>.editorconfig</c> override.
    /// </summary>
    /// <remarks>
    /// A non-blocking style nudge, independent of - and well below - <see cref="MaxCaseTypes"/>'s hard,
    /// build-breaking ceiling: a union already inside the structural limit can still be a code smell, since a
    /// type or handler needing this many cases is usually a sign it's doing too much (see
    /// <see cref="InterlocutorDiagnostics.RecommendedOutcomeArityExceeded"/>'s own description). Six is a
    /// judgment call about readability, not derived from any property of the shipped <c>Outcome</c>/
    /// <c>Outcome&lt;...&gt;</c> family, whose own ceiling matches <see cref="MaxCaseTypes"/> instead.
    /// </remarks>
    private const int DefaultMaxRecommendedCaseTypes = 6;

    /// <summary>
    /// The <c>.editorconfig</c>-style analyzer option key <see cref="ReadMaxRecommendedCaseTypes"/> reads to
    /// override <see cref="DefaultMaxRecommendedCaseTypes"/>.
    /// </summary>
    private const string MaxRecommendedArityOptionName = "interlocutor_max_recommended_outcome_arity";

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

        var modelsWithOptions = models.Combine(context.AnalyzerConfigOptionsProvider);

        context.RegisterSourceOutput(
            modelsWithOptions,
            static (productionContext, pair) => Execute(productionContext, pair.Left, pair.Right)
        );
    }

    /// <summary>
    /// Reports <see cref="InterlocutorDiagnostics.TooFewOutcomeCaseTypes"/> for an under-arity attributed
    /// union, <see cref="InterlocutorDiagnostics.TooManyOutcomeCaseTypes"/> for an over-arity one,
    /// <see cref="InterlocutorDiagnostics.RecommendedOutcomeArityExceeded"/> when the case count exceeds the
    /// configured recommended maximum (non-blocking), <see cref="InterlocutorDiagnostics.AccessorNameCollision"/>
    /// for a colliding named accessor, or otherwise emits its generated partial declaration via
    /// <see cref="OutcomeMemberEmitter"/>.
    /// </summary>
    /// <param name="context">The source production context to add the generated source (or diagnostics) to.</param>
    /// <param name="model">The attributed union to process.</param>
    /// <param name="optionsProvider">
    /// The compilation's <c>.editorconfig</c>-style analyzer options, consulted for
    /// <see cref="MaxRecommendedArityOptionName"/>.
    /// </param>
    private static void Execute(
        SourceProductionContext context,
        OutcomeUnionModel model,
        AnalyzerConfigOptionsProvider optionsProvider
    )
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

        if (model.CaseTypeDisplayStrings.Length > MaxCaseTypes)
        {
            context.ReportDiagnostic(
                Diagnostic.Create(
                    InterlocutorDiagnostics.TooManyOutcomeCaseTypes,
                    model.Location,
                    model.DisplayName,
                    model.CaseTypeDisplayStrings.Length
                )
            );
            return;
        }

        var maxRecommendedCaseTypes = ReadMaxRecommendedCaseTypes(optionsProvider, model.Location);
        if (model.CaseTypeDisplayStrings.Length > maxRecommendedCaseTypes)
        {
            // A style nudge, not a structural limit - unlike the two checks above, this never gates emission.
            context.ReportDiagnostic(
                Diagnostic.Create(
                    InterlocutorDiagnostics.RecommendedOutcomeArityExceeded,
                    model.Location,
                    model.DisplayName,
                    model.CaseTypeDisplayStrings.Length,
                    maxRecommendedCaseTypes
                )
            );
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

    /// <summary>
    /// Reads <see cref="MaxRecommendedArityOptionName"/> from the <c>.editorconfig</c>-style options that apply
    /// to <paramref name="location"/>'s own source file, falling back to <see cref="DefaultMaxRecommendedCaseTypes"/>
    /// when the option is absent, not a valid positive integer, or the declaration's syntax tree is unavailable.
    /// </summary>
    /// <param name="optionsProvider">The compilation's analyzer config options.</param>
    /// <param name="location">The attributed union declaration's location, used to select the applicable file's options.</param>
    /// <returns>The configured (or default) recommended maximum case-type count.</returns>
    private static int ReadMaxRecommendedCaseTypes(AnalyzerConfigOptionsProvider optionsProvider, Location location)
    {
        var options = location.SourceTree is { } sourceTree
            ? optionsProvider.GetOptions(sourceTree)
            : optionsProvider.GlobalOptions;

        return
            options.TryGetValue(MaxRecommendedArityOptionName, out var rawValue)
            && int.TryParse(rawValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var configuredValue)
            && configuredValue > 0
            ? configuredValue
            : DefaultMaxRecommendedCaseTypes;
    }
}
