namespace Interlocutor.Generator.Tests;

using System.Linq;

/// <summary>
/// Covers the incremental-pipeline caching claim in <see cref="CompilationInfo"/>'s remarks: the
/// <c>"CompilationInfo"</c>-tracked pipeline step (referenced-module discovery plus this compilation's own
/// namespace segment) reports <see cref="Microsoft.CodeAnalysis.IncrementalStepRunReason.Unchanged"/>, not
/// <see cref="Microsoft.CodeAnalysis.IncrementalStepRunReason.New"/>, when a rebuild's own source changes in a
/// way that leaves the reference graph and assembly name untouched - <c>Unchanged</c>, not
/// <see cref="Microsoft.CodeAnalysis.IncrementalStepRunReason.Cached"/>, because
/// <see cref="Microsoft.CodeAnalysis.Compilation"/> itself is a fresh object on every run (so the step's
/// transform function does re-execute), but the *output*
/// it produces compares equal to the previous run's, which is what lets any further downstream step depending
/// only on this one skip re-computing in turn.
/// </summary>
/// <remarks>
/// This is a regression test for a code-review finding on this ticket's own merge:
/// <see cref="CompilationInfo"/> and <see cref="ExternalModuleInfo"/> originally relied on compiler-generated
/// positional-record equality, which is useless for driver caching because <see cref="System.Collections.Immutable.ImmutableArray{T}"/>
/// compares its backing array by reference, not by element - two separately-built, content-identical arrays
/// never compared equal, so this step would have reported <c>New</c> on every run regardless of what actually
/// changed, and this test would have failed before that fix.
/// </remarks>
public sealed class IncrementalCachingTests
{
    private const string FirstSource = """
        namespace App;

        using System.Threading;
        using System.Threading.Tasks;
        using Interlocutor.Abstractions.Queries;
        using Interlocutor.CommonKernel;

        public sealed class GetFooQuery : IQuery<OperationOutcome>;

        public sealed class GetFooQueryHandler : IQueryHandler<GetFooQuery, OperationOutcome>
        {
            public Task<OperationOutcome> Handle(GetFooQuery query, CancellationToken ct) => throw new System.NotImplementedException();
        }
        """;

    // Whitespace-only edit: the reference graph and assembly name are unchanged, so ReferencedModuleDiscovery's
    // result and the sanitized namespace segment - everything the "CompilationInfo" step reads - are identical
    // to the first run's, even though this is a structurally different SyntaxTree/Compilation object.
    private const string SecondSource = FirstSource + "\n// a trailing comment, nothing else\n";

    [Fact]
    public void CompilationInfoStep_ReportsUnchanged_WhenReferenceGraphAndAssemblyNameAreUnchanged()
    {
        // Act
        var result = GeneratorTestHarness.RunTwiceTrackingSteps(FirstSource, SecondSource);

        // Assert
        var steps = result.Results.Single().TrackedSteps[InterlocutorSourceGenerator.CompilationInfoTrackingName];

        Assert.NotEmpty(steps);
        Assert.All(
            steps,
            step =>
                Assert.All(
                    step.Outputs,
                    output => Assert.Equal(Microsoft.CodeAnalysis.IncrementalStepRunReason.Unchanged, output.Reason)
                )
        );
    }
}
