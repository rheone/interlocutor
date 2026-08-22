namespace Interlocutor.Generator.Tests;

using System.Linq;

/// <summary>
/// Covers ticket 53's acceptance criteria: the pipeline's final, fully-combined step - the one feeding
/// <c>RegisterSourceOutput</c> - reports <see cref="Microsoft.CodeAnalysis.IncrementalStepRunReason.Cached"/>
/// or <see cref="Microsoft.CodeAnalysis.IncrementalStepRunReason.Unchanged"/> on a structurally-unchanged
/// rebuild, not <see cref="Microsoft.CodeAnalysis.IncrementalStepRunReason.New"/>. This is the whole-pipeline
/// claim <see cref="IncrementalCachingTests"/> could not make: that test only proves the narrower
/// <c>"CompilationInfo"</c> step is cached, while <see cref="InterlocutorSourceGenerator.Initialize"/>'s
/// combined pipeline still threaded the raw <see cref="Microsoft.CodeAnalysis.Compilation"/> through
/// unconditionally for open generic handler resolution (ticket 11) - and <see cref="Microsoft.CodeAnalysis.Compilation"/>
/// is never equal to itself across runs, which defeated caching for <c>RegisterSourceOutput</c> as a whole
/// regardless of how well-behaved every other input was. See <see cref="GenericConstraintSnapshot"/>'s remarks
/// for the fix: open generic handler resolution now consumes an equatable snapshot instead of the live
/// compilation.
/// </summary>
public sealed class FullPipelineIncrementalCachingTests
{
    private const string FirstSource = """
        namespace App;

        using System.Threading;
        using System.Threading.Tasks;
        using Interlocutor.Abstractions.Queries;
        using Interlocutor.CommonKernel;

        public interface IEntity;

        public sealed class Foo : IEntity
        {
            public Foo() { }
        }

        public sealed class GetFooQuery : IQuery<OperationOutcome>;

        public sealed class GetFooQueryHandler : IQueryHandler<GetFooQuery, OperationOutcome>
        {
            public Task<OperationOutcome> Handle(GetFooQuery query, CancellationToken ct) => throw new System.NotImplementedException();
        }

        public sealed class GenericQuery<T> : IQuery<OperationOutcome<T, string>>;

        public sealed class GenericQueryHandler<T> : IQueryHandler<GenericQuery<T>, OperationOutcome<T, string>>
            where T : IEntity, new()
        {
            public Task<OperationOutcome<T, string>> Handle(GenericQuery<T> query, CancellationToken ct) =>
                throw new System.NotImplementedException();
        }

        public sealed class UsesFoo
        {
            public void Use()
            {
                var query = new GenericQuery<Foo>();
            }
        }
        """;

    // Whitespace/comment-only edit: no candidate type declaration's syntax actually changes, so every input
    // to the final pipeline step should produce a value that compares equal to the previous run's.
    private const string SecondSource = FirstSource + "\n// a trailing comment, nothing else\n";

    private const string LibrarySource = """
        namespace Library;

        using System.Threading;
        using System.Threading.Tasks;
        using Interlocutor.Abstractions.Queries;
        using Interlocutor.CommonKernel;

        public sealed class GetBarQuery : IQuery<OperationOutcome>;

        public sealed class GetBarQueryHandler : IQueryHandler<GetBarQuery, OperationOutcome>
        {
            public Task<OperationOutcome> Handle(GetBarQuery query, CancellationToken ct) => throw new System.NotImplementedException();
        }
        """;

    // Method-body-only edit: the library's public API surface (types, members, signatures) is untouched, so
    // ReferencedModuleDiscovery and every fact GenericConstraintSnapshot could read from this reference are
    // unchanged too.
    private const string LibrarySourceWithChangedBody = """
        namespace Library;

        using System.Threading;
        using System.Threading.Tasks;
        using Interlocutor.Abstractions.Queries;
        using Interlocutor.CommonKernel;

        public sealed class GetBarQuery : IQuery<OperationOutcome>;

        public sealed class GetBarQueryHandler : IQueryHandler<GetBarQuery, OperationOutcome>
        {
            public Task<OperationOutcome> Handle(GetBarQuery query, CancellationToken ct)
            {
                throw new System.NotImplementedException();
            }
        }
        """;

    [Fact]
    public void FinalPipelineStep_ReportsCachedOrUnchanged_OnAWhitespaceOnlyRebuild()
    {
        var result = GeneratorTestHarness.RunTwiceTrackingStepsWithIncrementalEdit(FirstSource, SecondSource);

        AssertFinalStepCachedOrUnchanged(result);
    }

    [Fact]
    public void FinalPipelineStep_ReportsCachedOrUnchanged_WhenAReferencedLibraryChangesNoPublicApiSurface()
    {
        const string appSource = "namespace App; public sealed class Nothing;";

        var firstLibraryOutput = GeneratorTestHarness.RunAndUpdate(LibrarySource, "Library", null, out _);
        var firstLibraryReference = GeneratorTestHarness.ToMetadataReference(firstLibraryOutput);
        var firstAppResult = GeneratorTestHarness.Run(appSource, [firstLibraryReference]);
        var firstGeneratedSource = GetGeneratedSource(firstAppResult, "InterlocutorGeneratedRegistrations.g.cs");

        var secondLibraryOutput = GeneratorTestHarness.RunAndUpdate(LibrarySourceWithChangedBody, "Library", null, out _);
        var secondLibraryReference = GeneratorTestHarness.ToMetadataReference(secondLibraryOutput);
        var secondAppResult = GeneratorTestHarness.RunTwiceTrackingSteps(appSource, appSource, [secondLibraryReference]);

        AssertFinalStepCachedOrUnchanged(secondAppResult);

        var secondGeneratedSource = GetGeneratedSource(secondAppResult, "InterlocutorGeneratedRegistrations.g.cs");
        Assert.Equal(firstGeneratedSource, secondGeneratedSource);
    }

    private static void AssertFinalStepCachedOrUnchanged(Microsoft.CodeAnalysis.GeneratorDriverRunResult result)
    {
        var steps = result.Results.Single().TrackedSteps[InterlocutorSourceGenerator.FinalPipelineTrackingName];

        Assert.NotEmpty(steps);
        Assert.All(
            steps,
            step =>
                Assert.All(
                    step.Outputs,
                    output =>
                        Assert.True(
                            output.Reason
                                is Microsoft.CodeAnalysis.IncrementalStepRunReason.Cached
                                    or Microsoft.CodeAnalysis.IncrementalStepRunReason.Unchanged,
                            $"Expected Cached or Unchanged but was {output.Reason}."
                        )
                )
        );
    }

    private static string GetGeneratedSource(Microsoft.CodeAnalysis.GeneratorDriverRunResult result, string hintNameSuffix) =>
        result
            .Results.Single()
            .GeneratedSources.Single(source => source.HintName.EndsWith(hintNameSuffix, System.StringComparison.Ordinal))
            .SourceText.ToString();
}
