namespace Interlocutor.Generator.Tests.Emission;

using Microsoft.CodeAnalysis;

/// <summary>
/// Snapshot tests proving the generator's emitted dispatch wrappers catch
/// <c>Interlocutor.Abstractions.Pipeline.PipelineEjectException&lt;TResponse&gt;</c> around both the handler
/// call and each behavior call, matching the hand-written wrappers' (<c>QueryHandlerWrapper</c>,
/// <c>CommandHandlerWrapper</c>, <c>BareCommandHandlerWrapper</c>) identical try/catch.
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
    public void DispatchWrapper_EmitsCatchAroundHandlerCall_Test()
    {
        // Arrange
        // (no separate setup; uses class fixtures or direct act/assert)
        // Act
        var result = GeneratorTestHarness.Run(SingleQueryHandlerSource);

        // Assert
        var dispatchSource = GetGeneratedSource(result, "InterlocutorGeneratedDispatch.g.cs");

        Assert.Contains("pipeline = async () =>", dispatchSource, StringComparison.Ordinal);
        Assert.Contains(
            "return await handler.HandleAsync(request, ct).ConfigureAwait(false);",
            dispatchSource,
            StringComparison.Ordinal
        );
        Assert.Contains(
            "catch (global::Interlocutor.Abstractions.Pipeline.PipelineEjectException<global::Interlocutor.CommonKernel.Outcome> eject)",
            dispatchSource,
            StringComparison.Ordinal
        );
        Assert.Contains("return eject.Response;", dispatchSource, StringComparison.Ordinal);
    }

    [Fact]
    public void DispatchWrapper_EmitsCatchAroundBehaviorCall_Test()
    {
        // Arrange
        // (no separate setup; uses class fixtures or direct act/assert)
        // Act
        var result = GeneratorTestHarness.Run(SingleQueryHandlerSource);

        // Assert - the per-behavior fold closure gets the identical catch, distinct from the handler's own.
        var dispatchSource = GetGeneratedSource(result, "InterlocutorGeneratedDispatch.g.cs");

        Assert.Contains(
            "return await behavior.HandleAsync(request, next, ct).ConfigureAwait(false);",
            dispatchSource,
            StringComparison.Ordinal
        );

        var occurrences = CountOccurrences(
            dispatchSource,
            "catch (global::Interlocutor.Abstractions.Pipeline.PipelineEjectException<global::Interlocutor.CommonKernel.Outcome> eject)"
        );
        Assert.Equal(2, occurrences); // one around the handler call, one around the behavior call
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

    private static int CountOccurrences(string source, string needle)
    {
        var count = 0;
        var index = 0;
        while ((index = source.IndexOf(needle, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += needle.Length;
        }

        return count;
    }

    private static string GetGeneratedSource(GeneratorDriverRunResult result, string hintNameSuffix) =>
        result
            .Results.Single()
            .GeneratedSources.Single(s => s.HintName.EndsWith(hintNameSuffix, StringComparison.Ordinal))
            .SourceText.ToString();
}
