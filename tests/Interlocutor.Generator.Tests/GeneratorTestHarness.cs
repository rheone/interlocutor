namespace Interlocutor.Generator.Tests;

using System.Collections.Generic;
using System.Collections.Immutable;
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
