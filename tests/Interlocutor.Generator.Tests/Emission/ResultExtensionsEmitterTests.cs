namespace Interlocutor.Generator.Tests.Emission;

using System;
using System.Linq;
using System.Reflection;
using Interlocutor.AspNetCore;
using Interlocutor.Generator;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

/// <summary>
/// Table-driven emission tests for <c>Interlocutor.Generator.Emission.ResultExtensionsEmitter</c> - the
/// generator behind <see cref="Interlocutor.AspNetCore.ResultExtensions"/>'s per-arity <c>ToActionResult</c>/<c>ToMinimalApiResult</c>
/// overloads (the non-generic void-success <c>Outcome</c> overload, plus every <c>Outcome&lt;T1, ...&gt;</c>
/// arity from 2 through 16 - the shipped <c>Outcome</c> union family's own ceiling, matching the BCL's widest
/// <c>Func&lt;T1..T16, TResult&gt;</c>/<c>Action&lt;T1..T16&gt;</c> overload). Mirrors
/// <see cref="OutcomeMemberGeneratorTests"/>'s two-pronged approach: assert on the emitted source text's shape,
/// then compile and run it to prove the emitted overloads actually dispatch to the right case.
/// </summary>
/// <remarks>
/// <para>
/// <c>ResultExtensionsEmitter</c> and the <c>GenerateResultExtensionsAttribute</c> it targets are both
/// <see langword="internal"/> to their own assemblies, with no <c>InternalsVisibleTo</c> into this test project
/// - deliberately: the attribute is "internal, not part of this package's public extensibility surface... there
/// is no consumer-declared class this attribute is meant to be applied to" (see its own remarks), so this test
/// project was never meant to write <c>[GenerateResultExtensions]</c> the way <see cref="OutcomeMemberGeneratorTests"/>
/// writes <c>[GenerateOutcomeMembers]</c> against the public generator attribute. Instead, this class's private
/// <c>Emit</c> helper invokes <c>ResultExtensionsEmitter.Emit</c> directly via reflection - the same internal-type-without-IVT
/// workaround <c>OutcomeMemberEmitterReservedNamesTests</c> already uses for <c>OutcomeMemberEmitter</c>.
/// </para>
/// <para>
/// The compiled-behavior check below feeds the one piece of emitted text <see cref="GeneratedSource"/> (which
/// always contains every arity, exactly like the real generator always emits the full boilerplate for its one
/// attributed target) into <see cref="GeneratorTestHarness.EmitAndLoadAssembly(IIncrementalGenerator, string, string, System.Collections.Generic.IEnumerable{MetadataReference}?, LanguageVersion)"/>
/// alongside a small hand-written scaffold reproducing the private helpers the real, hand-written half of
/// <see cref="Interlocutor.AspNetCore.ResultExtensions"/> normally supplies - the emitted <c>Match</c> arms call
/// these by name (see that partial's own remarks) - proving the emitted overloads are not just textually
/// plausible but real, type-checked C# that dispatches correctly against the real
/// <see cref="Interlocutor.CommonKernel.Outcome{T1,T2}"/> family and <see cref="DefaultHttpOutcomeStatusMapper"/>.
/// </para>
/// </remarks>
public sealed class ResultExtensionsEmitterTests
{
    /// <summary>The synthetic attributed type's name, standing in for the real <c>ResultExtensions</c>.</summary>
    private const string TypeName = "GeneratedResultExtensions";

    /// <summary>
    /// The one emission this whole test class asserts against - built once, since <c>ResultExtensionsEmitter.Emit</c>
    /// always emits every arity (2 through 16) plus the void overload for its one target in a single call;
    /// there is no per-arity entry point to invoke separately.
    /// </summary>
    private static readonly string GeneratedSource = Emit();

    /// <summary>
    /// A <c>using</c> alias for the shipped ceiling arity (16 cases), used by <see cref="Scaffold"/>'s arity-16
    /// probes. Combined ahead of <see cref="GeneratedSource"/> (see <see cref="LoadProbe"/> and
    /// <c>Emit_CompiledAgainstRealApi_HasNoErrorDiagnostics_Test</c>), since a <c>using</c> directive must
    /// precede every type declaration in its compilation unit and <see cref="GeneratedSource"/> already opens
    /// with one. Every type argument is distinct - required for the union's implicit-conversion-based
    /// construction to stay unambiguous - with a recognized failure at the first (T2) and last (T16) slots and
    /// a plain <see cref="int"/> at a middle slot (T9) for the unrecognized-type probe; every other slot is
    /// inert filler.
    /// </summary>
    private const string ScaffoldUsings = """
        using SixteenArityOutcome = global::Interlocutor.CommonKernel.Outcome<
            string,
            global::Interlocutor.CommonKernel.ResultTypes.NotFound,
            bool,
            double,
            long,
            short,
            byte,
            char,
            int,
            float,
            decimal,
            global::System.DateTime,
            global::System.Guid,
            global::System.TimeSpan,
            uint,
            global::Interlocutor.CommonKernel.ResultTypes.RateLimited
        >;
        """;

    /// <summary>
    /// The hand-written half a real <c>[GenerateResultExtensions]</c> target normally supplies: the
    /// <c>DefaultMapper</c> field and the <c>Classify*</c>/<c>IsRecognizedFailureCase</c> helpers the emitted
    /// <c>Match</c> arms call by name (mirroring <see cref="Interlocutor.AspNetCore.ResultExtensions"/>'s own
    /// hand-written partial - see its remarks), plus a probe factory that calls the emitted overloads with known
    /// case values and known case positions and hands the results back as ordinary typed return values. The
    /// arity-2 and arity-16 (the shipped ceiling) probes use recognized failure shapes (<c>NotFound</c>/<c>Forbidden</c>/<c>RateLimited</c>)
    /// for their failure cases, and a plain <see cref="int"/> for the probes exercising the "unrecognized
    /// failure-slot type with no mapper" throw - the same "unrecognized failure type" scenario
    /// <c>ResultExtensionsTests.ToActionResult_WithUnrecognizedFailureSlotTypeAndNoMapper_ThrowsOutcomeCaseNotMappableException</c>
    /// exercises against the real, shipped <see cref="Interlocutor.AspNetCore.ResultExtensions"/>. The void
    /// probe has no failure case at all - the case-less <c>Outcome</c> acknowledgment always renders 204,
    /// unconditionally, with no mapper involved.
    /// </summary>
    private const string Scaffold = """
        public static partial class GeneratedResultExtensions
        {
            // Every emitted Match arm calls one of these by name - the real ResultExtensions provides them in
            // its own hand-written partial (see its own remarks); reproduced verbatim here since the emitted
            // text alone has nothing to dispatch those calls to.
            extension(global::Interlocutor.AspNetCore.OutcomeHttpMapping mapping)
            {
                private global::Microsoft.AspNetCore.Mvc.IActionResult ToActionResult() =>
                    mapping.Body is null
                        ? new global::Microsoft.AspNetCore.Mvc.StatusCodeResult(mapping.StatusCode)
                        : new global::Microsoft.AspNetCore.Mvc.ObjectResult(mapping.Body) { StatusCode = mapping.StatusCode };

                private global::Microsoft.AspNetCore.Http.IResult ToMinimalApiResult() =>
                    mapping.Body is null
                        ? global::Microsoft.AspNetCore.Http.Results.StatusCode(mapping.StatusCode)
                        : global::Microsoft.AspNetCore.Http.Results.Json(mapping.Body, statusCode: mapping.StatusCode);
            }

            private static readonly global::Interlocutor.AspNetCore.IOutcomeStatusMapper DefaultMapper =
                new global::Interlocutor.AspNetCore.DefaultHttpOutcomeStatusMapper();

            private static bool IsRecognizedFailureCase(object caseValue) =>
                caseValue switch
                {
                    global::Interlocutor.CommonKernel.ResultTypes.ValidationErrors
                    or global::Interlocutor.CommonKernel.ResultTypes.IValidationFailureCase => true,
                    global::Interlocutor.CommonKernel.ResultTypes.Forbidden
                    or global::Interlocutor.CommonKernel.ResultTypes.IForbiddenFailureCase => true,
                    global::Interlocutor.CommonKernel.ResultTypes.Conflict => true,
                    global::Interlocutor.CommonKernel.ResultTypes.Failures
                    or global::Interlocutor.CommonKernel.ResultTypes.Failure => true,
                    global::Interlocutor.CommonKernel.ResultTypes.NotFound => true,
                    global::Interlocutor.CommonKernel.ResultTypes.Cancelled => true,
                    global::Interlocutor.CommonKernel.ResultTypes.Timeout
                    or global::Interlocutor.CommonKernel.ResultTypes.ITimeoutFailureCase => true,
                    global::Interlocutor.CommonKernel.ResultTypes.RateLimited
                    or global::Interlocutor.CommonKernel.ResultTypes.IRateLimitedFailureCase => true,
                    _ => false,
                };

            private static global::Microsoft.AspNetCore.Mvc.IActionResult ClassifySuccessCaseForActionResult<TSuccess>(
                TSuccess value,
                global::Interlocutor.AspNetCore.IOutcomeStatusMapper? mapper
            ) =>
                value is not null && IsRecognizedFailureCase(value)
                    ? (mapper ?? DefaultMapper).Map(value).ToActionResult()
                    : new global::Microsoft.AspNetCore.Mvc.OkObjectResult(value);

            private static global::Microsoft.AspNetCore.Http.IResult ClassifySuccessCaseForMinimalApiResult<TSuccess>(
                TSuccess value,
                global::Interlocutor.AspNetCore.IOutcomeStatusMapper? mapper
            ) =>
                value is not null && IsRecognizedFailureCase(value)
                    ? (mapper ?? DefaultMapper).Map(value).ToMinimalApiResult()
                    : global::Microsoft.AspNetCore.Http.Results.Ok(value);

            private static global::Microsoft.AspNetCore.Mvc.IActionResult ClassifyFailureCaseForActionResult<TFailure>(
                TFailure value,
                global::Interlocutor.AspNetCore.IOutcomeStatusMapper? mapper,
                global::System.Type responseType
            ) =>
                mapper is not null || (value is not null && IsRecognizedFailureCase(value))
                    ? (mapper ?? DefaultMapper).Map(value).ToActionResult()
                    : throw new global::Interlocutor.AspNetCore.OutcomeCaseNotMappableException(
                        responseType,
                        value?.GetType() ?? typeof(TFailure),
                        "unmappable"
                    );

            private static global::Microsoft.AspNetCore.Http.IResult ClassifyFailureCaseForMinimalApiResult<TFailure>(
                TFailure value,
                global::Interlocutor.AspNetCore.IOutcomeStatusMapper? mapper,
                global::System.Type responseType
            ) =>
                mapper is not null || (value is not null && IsRecognizedFailureCase(value))
                    ? (mapper ?? DefaultMapper).Map(value).ToMinimalApiResult()
                    : throw new global::Interlocutor.AspNetCore.OutcomeCaseNotMappableException(
                        responseType,
                        value?.GetType() ?? typeof(TFailure),
                        "unmappable"
                    );
        }

        public static class ResultExtensionsEmitterProbe
        {
            public static global::Microsoft.AspNetCore.Mvc.IActionResult VoidAction() =>
                new global::Interlocutor.CommonKernel.Outcome().ToActionResult();

            public static global::Microsoft.AspNetCore.Http.IResult VoidMinimal() =>
                new global::Interlocutor.CommonKernel.Outcome().ToMinimalApiResult();

            public static global::Microsoft.AspNetCore.Mvc.IActionResult Arity2SuccessAction() =>
                new global::Interlocutor.CommonKernel.Outcome<string, global::Interlocutor.CommonKernel.ResultTypes.NotFound>(
                    "ok"
                ).ToActionResult();

            public static global::Microsoft.AspNetCore.Mvc.IActionResult Arity2FailureAction() =>
                new global::Interlocutor.CommonKernel.Outcome<string, global::Interlocutor.CommonKernel.ResultTypes.NotFound>(
                    new global::Interlocutor.CommonKernel.ResultTypes.NotFound("missing")
                ).ToActionResult();

            public static global::Microsoft.AspNetCore.Http.IResult Arity2SuccessMinimal() =>
                new global::Interlocutor.CommonKernel.Outcome<string, global::Interlocutor.CommonKernel.ResultTypes.NotFound>(
                    "ok"
                ).ToMinimalApiResult();

            public static global::Microsoft.AspNetCore.Http.IResult Arity2FailureMinimal() =>
                new global::Interlocutor.CommonKernel.Outcome<string, global::Interlocutor.CommonKernel.ResultTypes.NotFound>(
                    new global::Interlocutor.CommonKernel.ResultTypes.NotFound("missing")
                ).ToMinimalApiResult();

            public static global::Microsoft.AspNetCore.Mvc.IActionResult Arity2UnrecognizedFailureNoMapperAction() =>
                new global::Interlocutor.CommonKernel.Outcome<string, int>(2).ToActionResult();

            // The shipped ceiling arity (16 cases, aliased to SixteenArityOutcome above): T1 success, T2 a
            // recognized failure (first slot), T9 a plain int (unrecognized, middle slot), T16 a recognized
            // failure (last slot) - every other slot is inert filler, distinct per the union's
            // implicit-conversion construction requirement.
            public static global::Microsoft.AspNetCore.Mvc.IActionResult Arity16SuccessAction() =>
                new SixteenArityOutcome("ok").ToActionResult();

            public static global::Microsoft.AspNetCore.Mvc.IActionResult Arity16FirstFailureAction() =>
                new SixteenArityOutcome(new global::Interlocutor.CommonKernel.ResultTypes.NotFound("missing")).ToActionResult();

            public static global::Microsoft.AspNetCore.Mvc.IActionResult Arity16LastFailureAction() =>
                new SixteenArityOutcome(
                    new global::Interlocutor.CommonKernel.ResultTypes.RateLimited("per-user", global::System.TimeSpan.FromSeconds(1), null)
                ).ToActionResult();

            public static global::Microsoft.AspNetCore.Http.IResult Arity16LastFailureMinimal() =>
                new SixteenArityOutcome(
                    new global::Interlocutor.CommonKernel.ResultTypes.RateLimited("per-user", global::System.TimeSpan.FromSeconds(1), null)
                ).ToMinimalApiResult();

            public static global::Microsoft.AspNetCore.Mvc.IActionResult Arity16UnrecognizedFailureNoMapperAction() =>
                new SixteenArityOutcome(9).ToActionResult();
        }
        """;

    // --- Source shape: the void, non-generic overload ---

    [Fact]
    public void Emit_Void_EmitsNonGenericOutcomeExtensionAlwaysRenderingNoContent_Test()
    {
        Assert.Contains(
            "extension(global::Interlocutor.CommonKernel.Outcome result)",
            GeneratedSource,
            StringComparison.Ordinal
        );
        Assert.Contains("new global::Microsoft.AspNetCore.Mvc.NoContentResult();", GeneratedSource, StringComparison.Ordinal);
        Assert.Contains("global::Microsoft.AspNetCore.Http.Results.NoContent();", GeneratedSource, StringComparison.Ordinal);
    }

    // --- Source shape: one extension<...> declaration per arity, table-driven over arities 2-16 ---

    [Theory]
    [InlineData(2, "extension<T1, T2>(global::Interlocutor.CommonKernel.Outcome<T1, T2> result)")]
    [InlineData(3, "extension<T1, T2, T3>(global::Interlocutor.CommonKernel.Outcome<T1, T2, T3> result)")]
    [InlineData(4, "extension<T1, T2, T3, T4>(global::Interlocutor.CommonKernel.Outcome<T1, T2, T3, T4> result)")]
    [InlineData(5, "extension<T1, T2, T3, T4, T5>(global::Interlocutor.CommonKernel.Outcome<T1, T2, T3, T4, T5> result)")]
    [InlineData(
        6,
        "extension<T1, T2, T3, T4, T5, T6>(global::Interlocutor.CommonKernel.Outcome<T1, T2, T3, T4, T5, T6> result)"
    )]
    [InlineData(
        7,
        "extension<T1, T2, T3, T4, T5, T6, T7>(global::Interlocutor.CommonKernel.Outcome<T1, T2, T3, T4, T5, T6, T7> result)"
    )]
    [InlineData(
        8,
        "extension<T1, T2, T3, T4, T5, T6, T7, T8>(global::Interlocutor.CommonKernel.Outcome<T1, T2, T3, T4, T5, T6, T7, T8> result)"
    )]
    [InlineData(
        9,
        "extension<T1, T2, T3, T4, T5, T6, T7, T8, T9>(global::Interlocutor.CommonKernel.Outcome<T1, T2, T3, T4, T5, T6, T7, T8, T9> result)"
    )]
    [InlineData(
        10,
        "extension<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(global::Interlocutor.CommonKernel.Outcome<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> result)"
    )]
    [InlineData(
        11,
        "extension<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(global::Interlocutor.CommonKernel.Outcome<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> result)"
    )]
    [InlineData(
        12,
        "extension<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(global::Interlocutor.CommonKernel.Outcome<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> result)"
    )]
    [InlineData(
        13,
        "extension<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(global::Interlocutor.CommonKernel.Outcome<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13> result)"
    )]
    [InlineData(
        14,
        "extension<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(global::Interlocutor.CommonKernel.Outcome<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14> result)"
    )]
    [InlineData(
        15,
        "extension<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(global::Interlocutor.CommonKernel.Outcome<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15> result)"
    )]
    [InlineData(
        16,
        "extension<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>(global::Interlocutor.CommonKernel.Outcome<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16> result)"
    )]
    public void Emit_EachArity_EmitsExpectedExtensionDeclaration_Test(int arity, string expectedDeclaration)
    {
        _ = arity;
        Assert.Contains(expectedDeclaration, GeneratedSource, StringComparison.Ordinal);
    }

    // --- Source shape: every arity's success arm delegates to the shared Classify helper, table-driven ---

    [Theory]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    [InlineData(7)]
    [InlineData(8)]
    [InlineData(9)]
    [InlineData(10)]
    [InlineData(11)]
    [InlineData(12)]
    [InlineData(13)]
    [InlineData(14)]
    [InlineData(15)]
    [InlineData(16)]
    public void Emit_EachArity_EmitsSuccessArmDelegatingToClassifyHelper_Test(int arity)
    {
        _ = arity;
        Assert.Contains("ok => ClassifySuccessCaseForActionResult(ok, mapper),", GeneratedSource, StringComparison.Ordinal);
        Assert.Contains("ok => ClassifySuccessCaseForMinimalApiResult(ok, mapper),", GeneratedSource, StringComparison.Ordinal);
    }

    /// <summary>
    /// A failure-arm rendering line for case index <c>K</c> (<c>"failure"</c> for arity 2, <c>"caseK"</c> above
    /// that) appears once per emitted arity that actually has a case at that index - i.e. once for every arity
    /// from <c>K</c> through the emitter's own ceiling (16), except <c>K = 2</c> itself: arity 2 names its
    /// second case <c>"failure"</c>, not <c>"case2"</c>, so <c>case2</c>'s count matches <c>case3</c>'s rather
    /// than starting one arity earlier. The void overload has no failure arm at all (the case-less
    /// <c>Outcome</c> acknowledgment always renders 204 unconditionally - see
    /// <see cref="Emit_Void_EmitsNonGenericOutcomeExtensionAlwaysRenderingNoContent_Test"/>), so it contributes
    /// nothing to any of these counts. This is the single check most likely to catch an off-by-one in
    /// <c>ResultExtensionsEmitter.EmitFailureMatchArms</c>'s loop bound: a dropped or duplicated arm at some
    /// arity shows up as a count that doesn't match this table.
    /// </summary>
    /// <param name="caseName">The failure-arm parameter name to search for (<c>"failure"</c> or <c>"caseK"</c>).</param>
    /// <param name="expectedOccurrencesPerRenderMethod">
    /// The expected occurrence count of <paramref name="caseName"/>'s arm, once for <c>ToActionResult</c> and
    /// once for <c>ToMinimalApiResult</c> - see this test's own remarks for how each expected count is derived.
    /// </param>
    [Theory]
    [InlineData("failure", 1)]
    [InlineData("case2", 14)]
    [InlineData("case3", 14)]
    [InlineData("case4", 13)]
    [InlineData("case5", 12)]
    [InlineData("case6", 11)]
    [InlineData("case7", 10)]
    [InlineData("case8", 9)]
    [InlineData("case9", 8)]
    [InlineData("case10", 7)]
    [InlineData("case11", 6)]
    [InlineData("case12", 5)]
    [InlineData("case13", 4)]
    [InlineData("case14", 3)]
    [InlineData("case15", 2)]
    [InlineData("case16", 1)]
    public void Emit_EachCaseIndex_EmitsExactlyOneFailureArmPerArityThatHasIt_Test(
        string caseName,
        int expectedOccurrencesPerRenderMethod
    )
    {
        Assert.Equal(
            expectedOccurrencesPerRenderMethod,
            CountOccurrences(GeneratedSource, $"{caseName} => ClassifyFailureCaseForActionResult({caseName}, mapper, typeof(")
        );
        Assert.Equal(
            expectedOccurrencesPerRenderMethod,
            CountOccurrences(
                GeneratedSource,
                $"{caseName} => ClassifyFailureCaseForMinimalApiResult({caseName}, mapper, typeof("
            )
        );
    }

    // --- Compiled/runtime behavior: the emitted text, compiled for real, dispatches to the right case ---

    [Fact]
    public void Emit_CompiledAgainstRealApi_HasNoErrorDiagnostics_Test()
    {
        // Arrange - ScaffoldUsings must precede GeneratedSource: a using directive has to come before every
        // type declaration in its compilation unit, and GeneratedSource already opens with one (see
        // ScaffoldUsings's own remarks).
        var combinedSource = ScaffoldUsings + Environment.NewLine + GeneratedSource + Environment.NewLine + Scaffold;

        // Act
        var compilation = GeneratorTestHarness.CreateCompilation(
            combinedSource,
            "ResultExtensionsEmitter.Tests.CompilesCleanly",
            [MetadataReference.CreateFromFile(typeof(IOutcomeStatusMapper).Assembly.Location)],
            LanguageVersion.Preview
        );

        // Assert - the emitted overloads type-check against the real Outcome<...> family and the real
        // IOutcomeStatusMapper/IActionResult/IResult surface, not merely against hand-matched substrings.
        var errors = compilation.GetDiagnostics(TestContext.Current.CancellationToken);
        Assert.Empty(errors.Where(static d => d.Severity == DiagnosticSeverity.Error));
    }

    [Fact]
    public void Emit_CompiledAndLoaded_VoidOverload_AlwaysRendersNoContent_Test()
    {
        // Arrange
        var probe = LoadProbe("ResultExtensionsEmitter.Tests.VoidRuntime");

        // Act
        var action = (IActionResult)probe.GetMethod("VoidAction")!.Invoke(null, null)!;
        var minimal = (IResult)probe.GetMethod("VoidMinimal")!.Invoke(null, null)!;

        // Assert - the case-less Outcome has no failure case to map, so both overloads always render 204,
        // unconditionally, with no mapper ever consulted.
        Assert.IsType<NoContentResult>(action);
        Assert.Equal("NoContent", minimal.GetType().Name);
    }

    [Fact]
    public void Emit_CompiledAndLoaded_ArityTwo_DispatchesSuccessAndFailure_Test()
    {
        // Arrange - the minimum arity: one success case, one recognized failure case (NotFound).
        var probe = LoadProbe("ResultExtensionsEmitter.Tests.ArityTwoRuntime");

        // Act
        var success = (IActionResult)probe.GetMethod("Arity2SuccessAction")!.Invoke(null, null)!;
        var failure = (IActionResult)probe.GetMethod("Arity2FailureAction")!.Invoke(null, null)!;
        var successMinimal = (IResult)probe.GetMethod("Arity2SuccessMinimal")!.Invoke(null, null)!;
        var failureMinimal = (IResult)probe.GetMethod("Arity2FailureMinimal")!.Invoke(null, null)!;

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(success);
        Assert.Equal("ok", okResult.Value);
        var objectResult = Assert.IsType<ObjectResult>(failure);
        Assert.Equal(StatusCodes.Status404NotFound, objectResult.StatusCode);
        Assert.Contains("Ok", successMinimal.GetType().Name, StringComparison.Ordinal);
        Assert.Equal(
            StatusCodes.Status404NotFound,
            (int?)failureMinimal.GetType().GetProperty("StatusCode")?.GetValue(failureMinimal)
        );
    }

    [Fact]
    public void Emit_CompiledAndLoaded_ArityTwo_UnrecognizedFailureSlotTypeWithNoMapper_ThrowsOutcomeCaseNotMappableException_Test()
    {
        // Arrange - a plain int occupying the failure slot, with no mapper supplied to take responsibility for
        // it, matches none of the recognized failure shapes - the emitted Match arm must refuse to guess a
        // status for it rather than silently falling through to a generic one.
        var probe = LoadProbe("ResultExtensionsEmitter.Tests.ArityTwoUnrecognizedRuntime");

        // Act & Assert
        var targetInvocationException = Assert.Throws<TargetInvocationException>(() =>
            probe.GetMethod("Arity2UnrecognizedFailureNoMapperAction")!.Invoke(null, null)
        );
        Assert.IsType<OutcomeCaseNotMappableException>(targetInvocationException.InnerException);
    }

    [Fact]
    public void Emit_CompiledAndLoaded_ArityMax_DispatchesSuccessAndBoundaryFailureCases_Test()
    {
        // Arrange - the maximum arity (16, the shipped Outcome family's own ceiling): success plus the first
        // (case2, NotFound) and last (case16, RateLimited) failure arms, the positions a loop-bound bug in
        // EmitFailureMatchArms is most likely to get wrong.
        var probe = LoadProbe("ResultExtensionsEmitter.Tests.ArityMaxRuntime");

        // Act
        var success = (IActionResult)probe.GetMethod("Arity16SuccessAction")!.Invoke(null, null)!;
        var firstFailure = (IActionResult)probe.GetMethod("Arity16FirstFailureAction")!.Invoke(null, null)!;
        var lastFailure = (IActionResult)probe.GetMethod("Arity16LastFailureAction")!.Invoke(null, null)!;
        var lastFailureMinimal = (IResult)probe.GetMethod("Arity16LastFailureMinimal")!.Invoke(null, null)!;

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(success);
        Assert.Equal("ok", okResult.Value);

        var firstObjectResult = Assert.IsType<ObjectResult>(firstFailure);
        Assert.Equal(StatusCodes.Status404NotFound, firstObjectResult.StatusCode);

        var lastObjectResult = Assert.IsType<ObjectResult>(lastFailure);
        Assert.Equal(StatusCodes.Status429TooManyRequests, lastObjectResult.StatusCode);

        Assert.Equal(
            StatusCodes.Status429TooManyRequests,
            (int?)lastFailureMinimal.GetType().GetProperty("StatusCode")?.GetValue(lastFailureMinimal)
        );
    }

    [Fact]
    public void Emit_CompiledAndLoaded_ArityMax_UnrecognizedFailureSlotTypeAtMiddleCaseWithNoMapper_ThrowsOutcomeCaseNotMappableException_Test()
    {
        // Arrange - a plain int occupying a middle failure slot (case9) at the shipped ceiling arity (16),
        // with no mapper supplied to take responsibility for it: the emitted Match arm must refuse to guess a
        // status for it, exactly like the arity-2 equivalent above - confirming the same refusal, resolved by
        // the shared Classify* helpers rather than any position-based logic, holds at every arity the emitter
        // generates, not only the smallest one.
        var probe = LoadProbe("ResultExtensionsEmitter.Tests.ArityMaxUnrecognizedRuntime");

        // Act & Assert
        var targetInvocationException = Assert.Throws<TargetInvocationException>(() =>
            probe.GetMethod("Arity16UnrecognizedFailureNoMapperAction")!.Invoke(null, null)
        );
        Assert.IsType<OutcomeCaseNotMappableException>(targetInvocationException.InnerException);
    }

    /// <summary>Invokes <c>ResultExtensionsEmitter.Emit</c> reflectively - see this class's own remarks.</summary>
    private static string Emit()
    {
        var generatorAssembly = typeof(ResultExtensionsGenerator).Assembly;

        var modelType = generatorAssembly.GetType(
            "Interlocutor.Generator.Models.ResultExtensionsTargetModel",
            throwOnError: true
        )!;
        // Records also generate a copy constructor (ResultExtensionsTargetModel original) alongside the
        // primary one - filter to the 4-parameter primary constructor specifically.
        var modelConstructor = modelType
            .GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .Single(static c => c.GetParameters().Length == 4);

        // Namespace left empty so the emitted class sits in the global namespace, alongside Scaffold below -
        // letting the two be concatenated into one compilation unit with no `using` (both fully qualify every
        // referenced type already) and no namespace-block gymnastics.
        var model = modelConstructor.Invoke([string.Empty, "public static partial", TypeName, $"{TypeName}.HttpResults.g.cs"]);

        var emitterType = generatorAssembly.GetType(
            "Interlocutor.Generator.Emission.ResultExtensionsEmitter",
            throwOnError: true
        )!;
        var emitMethod = emitterType.GetMethod("Emit", BindingFlags.Public | BindingFlags.Static)!;
        return (string)emitMethod.Invoke(null, [model])!;
    }

    /// <summary>
    /// Compiles <see cref="GeneratedSource"/> plus <see cref="Scaffold"/>, loads the result, and returns the
    /// probe factory type - the shared setup for every compiled-and-loaded runtime-behavior test above.
    /// </summary>
    /// <param name="assemblyName">
    /// A name unique to the calling test - see <see cref="GeneratorTestHarness.EmitAndLoadAssembly(string, string, System.Collections.Generic.IEnumerable{MetadataReference}?)"/>'s remarks.
    /// </param>
    private static Type LoadProbe(string assemblyName)
    {
        // ScaffoldUsings must precede GeneratedSource - see Emit_CompiledAgainstRealApi_HasNoErrorDiagnostics_Test's
        // own remarks.
        var combinedSource = ScaffoldUsings + Environment.NewLine + GeneratedSource + Environment.NewLine + Scaffold;

        // ResultExtensionsGenerator itself is passed as the driving generator purely as a harmless pass-through
        // - combinedSource declares no [GenerateResultExtensions]-attributed class (that attribute is internal
        // to Interlocutor.AspNetCore with no InternalsVisibleTo here, see this class's own remarks), so it
        // finds nothing to act on and contributes no output of its own.
        var (assembly, _) = GeneratorTestHarness.EmitAndLoadAssembly(
            new ResultExtensionsGenerator(),
            combinedSource,
            assemblyName,
            [MetadataReference.CreateFromFile(typeof(IOutcomeStatusMapper).Assembly.Location)],
            LanguageVersion.Preview
        );

        return assembly.GetType("ResultExtensionsEmitterProbe", throwOnError: true)!;
    }

    /// <summary>Counts non-overlapping occurrences of <paramref name="value"/> in <paramref name="text"/>.</summary>
    private static int CountOccurrences(string text, string value)
    {
        var count = 0;
        var index = 0;
        while ((index = text.IndexOf(value, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += value.Length;
        }

        return count;
    }
}
