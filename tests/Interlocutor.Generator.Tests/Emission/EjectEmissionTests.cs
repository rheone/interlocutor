namespace Interlocutor.Generator.Tests.Emission;

using Microsoft.CodeAnalysis;

/// <summary>
/// Snapshot tests proving the generator's emitted dispatch wrappers delegate their fold/eject-catch logic
/// entirely to <c>Interlocutor.Abstractions.Pipeline.PipelineChainExecutor.ExecuteAsync</c> (ticket 01) -
/// the same shared entry point the hand-written wrappers (<c>QueryHandlerWrapper</c>,
/// <c>CommandHandlerWrapper</c>, <c>BareCommandHandlerWrapper</c>) call - rather than re-deriving that fold as
/// string-templated source. Before ticket 01, the generator's own hand-templated copy of this logic only
/// caught <c>PipelineEjectException&lt;TResponse&gt;</c>, never <c>PipelineEjectCaseException</c> (the signal
/// <c>Eject.WithCase</c> throws), so it silently drifted from the hand-written path - see
/// <c>GeneratedDispatchEjectCaseEndToEndTests</c> for the runtime proof that the unified call recovers both
/// signals identically to the hand-written path.
/// </summary>
public sealed class EjectEmissionTests
{
    private const string SingleQueryHandlerSource = """
        namespace TestApp;

        using System.Threading;
        using System.Threading.Tasks;
        using Interlocutor.Abstractions.Queries;
        using Interlocutor.CommonKernel;

        public interface ISomeDependency;

        public sealed class GetFooQuery : IQuery<Outcome>;

        public sealed class GetFooQueryHandler(ISomeDependency dependency) : IQueryHandler<GetFooQuery, Outcome>
        {
            public Task<Outcome> HandleAsync(GetFooQuery query, CancellationToken ct) => throw new System.NotImplementedException();
        }
        """;

    [Fact]
    public void DispatchWrapper_DelegatesHandlerCallToPipelineChainExecutor_Test()
    {
        // Arrange
        // (no separate setup; uses class fixtures or direct act/assert)
        // Act
        var result = GeneratorTestHarness.Run(SingleQueryHandlerSource);

        // Assert - the emitted wrapper hands the resolved handler's bound HandleAsync straight to
        // PipelineChainExecutor.ExecuteAsync as the innermost pipeline delegate, rather than wrapping it in a
        // hand-templated try/catch of its own.
        var dispatchSource = GetGeneratedSource(result, "InterlocutorGeneratedDispatch.g.cs");

        Assert.Contains(
            "global::Interlocutor.Abstractions.Pipeline.PipelineChainExecutor.ExecuteAsync<global::TestApp.GetFooQuery, global::Interlocutor.CommonKernel.Outcome>(",
            dispatchSource,
            StringComparison.Ordinal
        );
        Assert.Contains("handler.HandleAsync,", dispatchSource, StringComparison.Ordinal);

        // No hand-templated fold/eject-catch logic left in the emitted source at all - PipelineChainExecutor
        // owns that entirely now (ticket 01), so neither of its historical markers appears here any more.
        Assert.DoesNotContain("pipeline = async () =>", dispatchSource, StringComparison.Ordinal);
        Assert.DoesNotContain("PipelineEjectException", dispatchSource, StringComparison.Ordinal);
    }

    [Fact]
    public void DispatchWrapper_PassesAmbientActivityToPipelineChainExecutor_Test()
    {
        // Arrange
        // (no separate setup; uses class fixtures or direct act/assert)
        // Act
        var result = GeneratorTestHarness.Run(SingleQueryHandlerSource);

        // Assert - the emitted static wrapper has no Activity parameter of its own (adding one would be a
        // breaking change to the public IGeneratedRequestDispatcher contract - see DispatchWrapperEmitter's
        // remarks), so it passes System.Diagnostics.Activity.Current through to PipelineChainExecutor, which
        // performs the eject tagging (ticket 05's MediatorActivitySource.EjectedTag) itself.
        var dispatchSource = GetGeneratedSource(result, "InterlocutorGeneratedDispatch.g.cs");

        Assert.Contains("global::System.Diagnostics.Activity.Current);", dispatchSource, StringComparison.Ordinal);
    }

    [Fact]
    public void GeneratedCode_WithEjectCatch_CompilesCleanly_Test()
    {
        // Arrange
        // (no separate setup; uses class fixtures or direct act/assert)
        // Act
        var outputCompilation = GeneratorTestHarness.RunAndUpdate(SingleQueryHandlerSource, out var generatorDiagnostics);

        // Assert
        Assert.Empty(generatorDiagnostics);

        var errors = outputCompilation
            .GetDiagnostics(TestContext.Current.CancellationToken)
            .Where(d => d.Severity == DiagnosticSeverity.Error)
            .ToList();
        Assert.Empty(errors);
    }

    private static string GetGeneratedSource(GeneratorDriverRunResult result, string hintNameSuffix) =>
        result
            .Results.Single()
            .GeneratedSources.Single(s => s.HintName.EndsWith(hintNameSuffix, StringComparison.Ordinal))
            .SourceText.ToString();
}
