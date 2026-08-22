namespace Interlocutor.Generator.Tests;

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Interlocutor.Abstractions.Queries;
using Interlocutor.CommonKernel;
using Interlocutor.DependencyInjection.Abstractions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

/// <summary>
/// Compiles a snippet of source against the real Interlocutor assemblies and runs
/// <see cref="InterlocutorSourceGenerator"/> over the resulting compilation, the way the Roslyn source-generator
/// testing package would, but hand-rolled directly against <see cref="CSharpGeneratorDriver"/>.
/// </summary>
/// <remarks>
/// The idiomatic <c>Microsoft.CodeAnalysis.CSharp.SourceGenerators.Testing.XUnit</c> package pulls xunit v2 as
/// a transitive dependency, which conflicts with this repository's xunit.v3-only convention (see
/// <c>CLAUDE.md</c>'s Commands section and every other test project's package references). Driving
/// <see cref="CSharpGeneratorDriver"/> directly gives the same snapshot/diagnostic assertions this ticket's
/// acceptance criteria ask for, with the same xunit.v3 test packages every other project in this repository uses.
/// </remarks>
internal static class GeneratorTestHarness
{
    private static readonly ImmutableArray<MetadataReference> References = BuildReferences();

    /// <summary>Builds an unaltered <see cref="CSharpCompilation"/> for <paramref name="source"/> with no generator run.</summary>
    /// <param name="source">The C# source to compile.</param>
    /// <param name="assemblyName">The compilation's assembly name.</param>
    /// <param name="extraReferences">
    /// Additional references beyond the base BCL and Interlocutor set - typically another test compilation's
    /// own output (see <see cref="RunAndUpdate(string, string, System.Collections.Generic.IEnumerable{MetadataReference}?, out ImmutableArray{Diagnostic})"/>),
    /// for simulating a project reference between two generator-run projects.
    /// </param>
    /// <returns>The resulting compilation.</returns>
    public static CSharpCompilation CreateCompilation(
        string source,
        string assemblyName = "Interlocutor.Generator.Tests.Compilation",
        IEnumerable<MetadataReference>? extraReferences = null
    )
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(
            source,
            new CSharpParseOptions(Microsoft.CodeAnalysis.CSharp.LanguageVersion.Latest)
        );
        var references = extraReferences is null ? References : References.AddRange(extraReferences);
        return CSharpCompilation.Create(
            assemblyName,
            [syntaxTree],
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
        );
    }

    /// <summary>
    /// Compiles <paramref name="source"/> and runs <see cref="InterlocutorSourceGenerator"/> over it, returning
    /// the generator driver's run result (generated trees and reported diagnostics) without merging the
    /// generated code back into a compilation.
    /// </summary>
    /// <param name="source">The C# source to compile and analyze.</param>
    /// <param name="extraReferences">Additional references - see <see cref="CreateCompilation"/>.</param>
    /// <returns>The generator driver's run result.</returns>
    public static GeneratorDriverRunResult Run(string source, IEnumerable<MetadataReference>? extraReferences = null)
    {
        var driver = CSharpGeneratorDriver.Create(new InterlocutorSourceGenerator());
        driver = (CSharpGeneratorDriver)driver.RunGenerators(CreateCompilation(source, extraReferences: extraReferences));
        return driver.GetRunResult();
    }

    /// <summary>
    /// Compiles <paramref name="source"/>, runs <see cref="InterlocutorSourceGenerator"/> over it, and returns
    /// the resulting compilation with the generated sources merged in - for asserting that the emitted code
    /// itself compiles cleanly alongside the source it was generated from, or for chaining into
    /// <see cref="ToMetadataReference"/> to simulate a project reference in a further compilation.
    /// </summary>
    /// <param name="source">The C# source to compile and analyze.</param>
    /// <param name="assemblyName">The compilation's assembly name.</param>
    /// <param name="extraReferences">Additional references - see <see cref="CreateCompilation"/>.</param>
    /// <param name="generatorDiagnostics">Diagnostics the generator itself reported (not compiler diagnostics).</param>
    /// <returns>The output compilation with the generator's emitted sources merged in.</returns>
    public static Compilation RunAndUpdate(
        string source,
        string assemblyName,
        IEnumerable<MetadataReference>? extraReferences,
        out ImmutableArray<Diagnostic> generatorDiagnostics
    )
    {
        var driver = CSharpGeneratorDriver.Create(new InterlocutorSourceGenerator());
        _ = driver.RunGeneratorsAndUpdateCompilation(
            CreateCompilation(source, assemblyName, extraReferences),
            out var outputCompilation,
            out generatorDiagnostics
        );
        return outputCompilation;
    }

    /// <summary>
    /// Compiles <paramref name="source"/> and runs <see cref="InterlocutorSourceGenerator"/> over it, using the
    /// default assembly name and no extra references. See
    /// <see cref="RunAndUpdate(string, string, System.Collections.Generic.IEnumerable{MetadataReference}?, out ImmutableArray{Diagnostic})"/>.
    /// </summary>
    /// <param name="source">The C# source to compile and analyze.</param>
    /// <param name="generatorDiagnostics">Diagnostics the generator itself reported (not compiler diagnostics).</param>
    /// <returns>The output compilation with the generator's emitted sources merged in.</returns>
    public static Compilation RunAndUpdate(string source, out ImmutableArray<Diagnostic> generatorDiagnostics) =>
        RunAndUpdate(source, "Interlocutor.Generator.Tests.Compilation", null, out generatorDiagnostics);

    /// <summary>
    /// Turns a compilation (typically the output of <see cref="RunAndUpdate(string, string, System.Collections.Generic.IEnumerable{MetadataReference}?, out ImmutableArray{Diagnostic})"/>)
    /// into a <see cref="MetadataReference"/> a further compilation can reference - simulating one project
    /// referencing another's already-built output, entirely in-memory and without emitting a real PE.
    /// </summary>
    /// <param name="compilation">The compilation to reference.</param>
    /// <returns>A metadata reference to <paramref name="compilation"/>.</returns>
    public static MetadataReference ToMetadataReference(Compilation compilation) => compilation.ToMetadataReference();

    /// <summary>
    /// Runs <see cref="InterlocutorSourceGenerator"/> over <paramref name="firstSource"/>, then re-runs the
    /// same driver instance over <paramref name="secondSource"/> - with <c>trackIncrementalGeneratorSteps: true</c>,
    /// so the second run's tracked steps report whether each named pipeline step was cached or re-computed.
    /// Used to test the incremental pipeline itself, not just its emitted output - see
    /// <see cref="CompilationInfo"/>'s remarks.
    /// </summary>
    /// <param name="firstSource">The source compiled and run first.</param>
    /// <param name="secondSource">The source the same driver instance is then re-run against.</param>
    /// <param name="extraReferences">Additional references shared by both runs - see <see cref="CreateCompilation"/>.</param>
    /// <returns>The second run's result, with incremental step tracking enabled.</returns>
    public static GeneratorDriverRunResult RunTwiceTrackingSteps(
        string firstSource,
        string secondSource,
        IEnumerable<MetadataReference>? extraReferences = null
    )
    {
        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            [new InterlocutorSourceGenerator().AsSourceGenerator()],
            driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true)
        );

        driver = driver.RunGenerators(CreateCompilation(firstSource, extraReferences: extraReferences));
        driver = driver.RunGenerators(CreateCompilation(secondSource, extraReferences: extraReferences));

        return driver.GetRunResult();
    }

    /// <summary>
    /// Like <see cref="RunTwiceTrackingSteps(string, string, IEnumerable{MetadataReference}?)"/>, but the second
    /// run's compilation is built as a genuine incremental edit of the first - <c>SyntaxTree.WithChangedText</c>
    /// applied to the first run's own tree, then swapped into the first run's own compilation via
    /// <c>Compilation.ReplaceSyntaxTree</c> - rather than two independently parsed compilations.
    /// </summary>
    /// <remarks>
    /// This distinction matters for per-syntax-node incremental generator steps specifically (ticket 53):
    /// Roslyn's incremental engine only recognizes an unaffected syntax subtree as reusable - and skips
    /// re-invoking a <c>CreateSyntaxProvider</c> transform for it entirely, reporting that step's output as
    /// <see cref="Microsoft.CodeAnalysis.IncrementalStepRunReason.Cached"/> - when the new tree shares green
    /// nodes with the old one via this kind of incremental edit. Two independently parsed
    /// <see cref="Microsoft.CodeAnalysis.SyntaxTree"/> instances (as <see cref="CreateCompilation"/> produces on
    /// each call) share no such ancestry even when their text is identical, so every candidate's transform
    /// re-runs regardless of what changed - which is the right way to test a *compilation-level* step like
    /// <see cref="CompilationInfo"/>'s (a fresh, unrelated compilation is exactly what a separate project
    /// reference or separate build invocation looks like), but the wrong way to test whether an edit untouched
    /// by a given candidate lets that candidate's own step report <c>Cached</c>.
    /// </remarks>
    /// <param name="firstSource">The source compiled and run first.</param>
    /// <param name="secondSource">The source the first run's own tree is incrementally edited into for the second run.</param>
    /// <param name="extraReferences">Additional references shared by both runs - see <see cref="CreateCompilation"/>.</param>
    /// <returns>The second run's result, with incremental step tracking enabled.</returns>
    public static GeneratorDriverRunResult RunTwiceTrackingStepsWithIncrementalEdit(
        string firstSource,
        string secondSource,
        IEnumerable<MetadataReference>? extraReferences = null
    )
    {
        var firstCompilation = CreateCompilation(firstSource, extraReferences: extraReferences);
        var firstTree = firstCompilation.SyntaxTrees.Single();
        var secondTree = firstTree.WithChangedText(Microsoft.CodeAnalysis.Text.SourceText.From(secondSource));
        var secondCompilation = firstCompilation.ReplaceSyntaxTree(firstTree, secondTree);

        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            [new InterlocutorSourceGenerator().AsSourceGenerator()],
            driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true)
        );

        driver = driver.RunGenerators(firstCompilation);
        driver = driver.RunGenerators(secondCompilation);

        return driver.GetRunResult();
    }

    /// <summary>
    /// Builds the reference assembly set every test compilation needs: the full trusted platform assembly
    /// list (BCL) plus the real, already-loaded Interlocutor assemblies the generator's discovery targets and
    /// its emitted code references.
    /// </summary>
    private static ImmutableArray<MetadataReference> BuildReferences()
    {
        var builder = ImmutableArray.CreateBuilder<MetadataReference>();

        var trustedPlatformAssemblies = (string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES");
        if (trustedPlatformAssemblies is not null)
        {
            foreach (var path in trustedPlatformAssemblies.Split(Path.PathSeparator))
            {
                builder.Add(MetadataReference.CreateFromFile(path));
            }
        }

        foreach (
            var assembly in new[]
            {
                typeof(IQuery<>).Assembly, // Interlocutor.Abstractions
                typeof(IOperationOutcome).Assembly, // Interlocutor.CommonKernel
                typeof(InterlocutorRegistration).Assembly, // Interlocutor.DependencyInjection.Abstractions
            }
        )
        {
            builder.Add(MetadataReference.CreateFromFile(assembly.Location));
        }

        return builder.ToImmutable();
    }
}
