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
/// overloads (the non-generic void-success <c>Outcome</c> overload, plus every <c>Outcome&lt;TSuccess, ...&gt;</c>
/// arity from 2 through 6). Mirrors <see cref="OutcomeMemberGeneratorTests"/>'s two-pronged approach: assert on
/// the emitted source text's shape, then compile and run it to prove the emitted overloads actually dispatch to
/// the right case.
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
/// alongside a small hand-written scaffold (a second partial declaration supplying the <c>DefaultMapper</c>
/// field the hand-written half of the real <see cref="Interlocutor.AspNetCore.ResultExtensions"/> normally provides, plus probe
/// factory methods) - proving the emitted overloads are not just textually plausible but real, type-checked C#
/// that dispatches correctly against the real <see cref="Interlocutor.CommonKernel.Outcome{TSuccessResult,TFailureResult}"/>
/// family and <see cref="DefaultHttpOutcomeStatusMapper"/>.
/// </para>
/// </remarks>
public sealed class ResultExtensionsEmitterTests
{
    /// <summary>The synthetic attributed type's name, standing in for the real <c>ResultExtensions</c>.</summary>
    private const string TypeName = "GeneratedResultExtensions";

    /// <summary>
    /// The one emission this whole test class asserts against - built once, since <c>ResultExtensionsEmitter.Emit</c>
    /// always emits every arity (2 through 6) plus the void overload for its one target in a single call; there
    /// is no per-arity entry point to invoke separately.
    /// </summary>
    private static readonly string GeneratedSource = Emit();

    /// <summary>
    /// The hand-written half a real <c>[GenerateResultExtensions]</c> target normally supplies (here, just the
    /// <c>DefaultMapper</c> field the emitted overloads' <c>mapper ?? DefaultMapper</c> reference needs - see
    /// <see cref="Interlocutor.AspNetCore.ResultExtensions"/>'s own remarks), plus a probe factory that calls the emitted overloads with
    /// known case values and known case positions and hands the results back as ordinary typed return values.
    /// The arity-2 and arity-6 probes' case types are pairwise distinct (<see cref="string"/>/<see cref="int"/>/
    /// <see cref="bool"/>/<see cref="double"/>/<see cref="char"/>/<see cref="long"/>) so each probe method's
    /// literal argument selects its case unambiguously, and none of these types are recognized by
    /// <see cref="DefaultHttpOutcomeStatusMapper"/>, so those failure probes deterministically render as
    /// <see cref="StatusCodes.Status500InternalServerError"/> with the failure value itself as the body - the
    /// same "unrecognized failure type" fallback <c>ResultExtensionsTests.ToActionResult_WithUnknownFailureType_Returns500</c>
    /// exercises against the real, shipped <see cref="Interlocutor.AspNetCore.ResultExtensions"/>. The void
    /// probe's failure case has no such freedom - the non-generic <c>Outcome</c> union's failure case is fixed
    /// to the built-in <see cref="Interlocutor.CommonKernel.ResultTypes.Failure"/> type - so it renders as
    /// <see cref="StatusCodes.Status400BadRequest"/> via <see cref="DefaultHttpOutcomeStatusMapper"/>'s own
    /// recognized-type mapping instead.
    /// </summary>
    private const string Scaffold = """
        public static partial class GeneratedResultExtensions
        {
            // Every emitted failure arm calls .ToActionResult()/.ToMinimalApiResult() on the OutcomeHttpMapping
            // (mapper ?? DefaultMapper).Map(...) returns - the real ResultExtensions provides these two private
            // rendering members in its own hand-written partial (see its own remarks); reproduced verbatim here
            // since the emitted text alone has nothing to dispatch those calls to.
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
        }

        public static class ResultExtensionsEmitterProbe
        {
            public static global::Microsoft.AspNetCore.Mvc.IActionResult VoidSuccessAction() =>
                new global::Interlocutor.CommonKernel.Outcome(default(global::Interlocutor.CommonKernel.ResultTypes.Success)).ToActionResult();

            public static global::Microsoft.AspNetCore.Mvc.IActionResult VoidFailureAction() =>
                new global::Interlocutor.CommonKernel.Outcome(new global::Interlocutor.CommonKernel.ResultTypes.Failure("boom")).ToActionResult();

            public static global::Microsoft.AspNetCore.Http.IResult VoidSuccessMinimal() =>
                new global::Interlocutor.CommonKernel.Outcome(default(global::Interlocutor.CommonKernel.ResultTypes.Success)).ToMinimalApiResult();

            public static global::Microsoft.AspNetCore.Mvc.IActionResult Arity2SuccessAction() =>
                new global::Interlocutor.CommonKernel.Outcome<string, int>("ok").ToActionResult();

            public static global::Microsoft.AspNetCore.Mvc.IActionResult Arity2FailureAction() =>
                new global::Interlocutor.CommonKernel.Outcome<string, int>(2).ToActionResult();

            public static global::Microsoft.AspNetCore.Http.IResult Arity2SuccessMinimal() =>
                new global::Interlocutor.CommonKernel.Outcome<string, int>("ok").ToMinimalApiResult();

            public static global::Microsoft.AspNetCore.Http.IResult Arity2FailureMinimal() =>
                new global::Interlocutor.CommonKernel.Outcome<string, int>(2).ToMinimalApiResult();

            public static global::Microsoft.AspNetCore.Mvc.IActionResult Arity6SuccessAction() =>
                new global::Interlocutor.CommonKernel.Outcome<string, int, bool, double, char, long>("ok").ToActionResult();

            public static global::Microsoft.AspNetCore.Mvc.IActionResult Arity6FirstFailureAction() =>
                new global::Interlocutor.CommonKernel.Outcome<string, int, bool, double, char, long>(2).ToActionResult();

            public static global::Microsoft.AspNetCore.Mvc.IActionResult Arity6LastFailureAction() =>
                new global::Interlocutor.CommonKernel.Outcome<string, int, bool, double, char, long>(600L).ToActionResult();

            public static global::Microsoft.AspNetCore.Http.IResult Arity6LastFailureMinimal() =>
                new global::Interlocutor.CommonKernel.Outcome<string, int, bool, double, char, long>(600L).ToMinimalApiResult();
        }
        """;

    // --- Source shape: the void, non-generic overload ---

    [Fact]
    public void Emit_Void_EmitsNonGenericOutcomeExtensionWithNoContentSuccess_Test()
    {
        Assert.Contains(
            "extension(global::Interlocutor.CommonKernel.Outcome result)",
            GeneratedSource,
            StringComparison.Ordinal
        );
        Assert.Contains(
            "static _ => new global::Microsoft.AspNetCore.Mvc.NoContentResult(),",
            GeneratedSource,
            StringComparison.Ordinal
        );
        Assert.Contains(
            "static _ => global::Microsoft.AspNetCore.Http.Results.NoContent(),",
            GeneratedSource,
            StringComparison.Ordinal
        );
    }

    // --- Source shape: one extension<...> declaration per arity, table-driven over arities 2-6 ---

    [Theory]
    [InlineData(2, "extension<TSuccess, TFailure>(global::Interlocutor.CommonKernel.Outcome<TSuccess, TFailure> result)")]
    [InlineData(
        3,
        "extension<TSuccess, TCase2, TCase3>(global::Interlocutor.CommonKernel.Outcome<TSuccess, TCase2, TCase3> result)"
    )]
    [InlineData(
        4,
        "extension<TSuccess, TCase2, TCase3, TCase4>(global::Interlocutor.CommonKernel.Outcome<TSuccess, TCase2, TCase3, TCase4> result)"
    )]
    [InlineData(
        5,
        "extension<TSuccess, TCase2, TCase3, TCase4, TCase5>(global::Interlocutor.CommonKernel.Outcome<TSuccess, TCase2, TCase3, TCase4, TCase5> result)"
    )]
    [InlineData(
        6,
        "extension<TSuccess, TCase2, TCase3, TCase4, TCase5, TCase6>(global::Interlocutor.CommonKernel.Outcome<TSuccess, TCase2, TCase3, TCase4, TCase5, TCase6> result)"
    )]
    public void Emit_EachArity_EmitsExpectedExtensionDeclaration_Test(int arity, string expectedDeclaration)
    {
        _ = arity;
        Assert.Contains(expectedDeclaration, GeneratedSource, StringComparison.Ordinal);
    }

    // --- Source shape: every arity's success arm renders OkObjectResult/Results.Ok, table-driven ---

    [Theory]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    public void Emit_EachArity_EmitsSuccessArmRenderingOk_Test(int arity)
    {
        _ = arity;
        Assert.Contains(
            "ok => new global::Microsoft.AspNetCore.Mvc.OkObjectResult(ok),",
            GeneratedSource,
            StringComparison.Ordinal
        );
        Assert.Contains("ok => global::Microsoft.AspNetCore.Http.Results.Ok(ok),", GeneratedSource, StringComparison.Ordinal);
    }

    /// <summary>
    /// A failure-arm rendering line for case index <c>K</c> (<c>"failure"</c> for the arity-2/void-shared name,
    /// <c>"caseK"</c> above that) appears once per emitted arity that actually has a case at that index - i.e.
    /// once for every arity from <c>K</c> through the emitter's own ceiling (6), except <c>K == 2</c>, whose
    /// "failure" name is shared by both the arity-2 block and the unrelated void block (2 occurrences: arity 2
    /// only has one failure case, and the void overload's own single failure arm happens to reuse the same
    /// parameter name). This is the single check most likely to catch an off-by-one in
    /// <c>ResultExtensionsEmitter.EmitFailureMatchArms</c>'s loop bound: a dropped or duplicated arm at some
    /// arity shows up as a count that doesn't match this table.
    /// </summary>
    /// <param name="caseName">The failure-arm parameter name to search for (<c>"failure"</c> or <c>"caseK"</c>).</param>
    /// <param name="expectedOccurrencesPerRenderMethod">
    /// The expected occurrence count of <paramref name="caseName"/>'s arm, once for <c>ToActionResult</c> and
    /// once for <c>ToMinimalApiResult</c> - see this test's own remarks for how each expected count is derived.
    /// </param>
    [Theory]
    [InlineData("failure", 2)]
    [InlineData("case2", 4)]
    [InlineData("case3", 4)]
    [InlineData("case4", 3)]
    [InlineData("case5", 2)]
    [InlineData("case6", 1)]
    public void Emit_EachCaseIndex_EmitsExactlyOneFailureArmPerArityThatHasIt_Test(
        string caseName,
        int expectedOccurrencesPerRenderMethod
    )
    {
        Assert.Equal(
            expectedOccurrencesPerRenderMethod,
            CountOccurrences(GeneratedSource, $"{caseName} => (mapper ?? DefaultMapper).Map({caseName}).ToActionResult()")
        );
        Assert.Equal(
            expectedOccurrencesPerRenderMethod,
            CountOccurrences(GeneratedSource, $"{caseName} => (mapper ?? DefaultMapper).Map({caseName}).ToMinimalApiResult()")
        );
    }

    // --- Compiled/runtime behavior: the emitted text, compiled for real, dispatches to the right case ---

    [Fact]
    public void Emit_CompiledAgainstRealApi_HasNoErrorDiagnostics_Test()
    {
        // Arrange
        var combinedSource = GeneratedSource + Environment.NewLine + Scaffold;

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
    public void Emit_CompiledAndLoaded_VoidOverload_DispatchesSuccessAndFailure_Test()
    {
        // Arrange
        var probe = LoadProbe("ResultExtensionsEmitter.Tests.VoidRuntime");

        // Act
        var success = (IActionResult)probe.GetMethod("VoidSuccessAction")!.Invoke(null, null)!;
        var failure = (IActionResult)probe.GetMethod("VoidFailureAction")!.Invoke(null, null)!;
        var successMinimal = (IResult)probe.GetMethod("VoidSuccessMinimal")!.Invoke(null, null)!;

        // Assert - the void Outcome's failure case is fixed to the built-in Failure type (unlike the other
        // probes below, which use unrecognized types deliberately), so DefaultHttpOutcomeStatusMapper maps it
        // to 400 via its own MapFailure branch rather than the MapUnrecognized fallback.
        Assert.IsType<NoContentResult>(success);
        var objectResult = Assert.IsType<ObjectResult>(failure);
        Assert.Equal(StatusCodes.Status400BadRequest, objectResult.StatusCode);
        Assert.Equal("NoContent", successMinimal.GetType().Name);
    }

    [Fact]
    public void Emit_CompiledAndLoaded_ArityTwo_DispatchesSuccessAndFailure_Test()
    {
        // Arrange - the minimum arity: one success case, one failure case.
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
        Assert.Equal(StatusCodes.Status500InternalServerError, objectResult.StatusCode);
        Assert.Equal(2, objectResult.Value);
        Assert.Contains("Ok", successMinimal.GetType().Name, StringComparison.Ordinal);
        Assert.Equal(
            StatusCodes.Status500InternalServerError,
            (int?)failureMinimal.GetType().GetProperty("StatusCode")?.GetValue(failureMinimal)
        );
    }

    [Fact]
    public void Emit_CompiledAndLoaded_ArityMax_DispatchesSuccessAndBoundaryFailureCases_Test()
    {
        // Arrange - the maximum arity (6): success plus the first (case2) and last (case6) failure arms, the
        // positions a loop-bound bug in EmitFailureMatchArms is most likely to get wrong.
        var probe = LoadProbe("ResultExtensionsEmitter.Tests.ArityMaxRuntime");

        // Act
        var success = (IActionResult)probe.GetMethod("Arity6SuccessAction")!.Invoke(null, null)!;
        var firstFailure = (IActionResult)probe.GetMethod("Arity6FirstFailureAction")!.Invoke(null, null)!;
        var lastFailure = (IActionResult)probe.GetMethod("Arity6LastFailureAction")!.Invoke(null, null)!;
        var lastFailureMinimal = (IResult)probe.GetMethod("Arity6LastFailureMinimal")!.Invoke(null, null)!;

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(success);
        Assert.Equal("ok", okResult.Value);

        var firstObjectResult = Assert.IsType<ObjectResult>(firstFailure);
        Assert.Equal(StatusCodes.Status500InternalServerError, firstObjectResult.StatusCode);
        Assert.Equal(2, firstObjectResult.Value);

        var lastObjectResult = Assert.IsType<ObjectResult>(lastFailure);
        Assert.Equal(StatusCodes.Status500InternalServerError, lastObjectResult.StatusCode);
        Assert.Equal(600L, lastObjectResult.Value);

        Assert.Equal(
            StatusCodes.Status500InternalServerError,
            (int?)lastFailureMinimal.GetType().GetProperty("StatusCode")?.GetValue(lastFailureMinimal)
        );
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
        var combinedSource = GeneratedSource + Environment.NewLine + Scaffold;

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
