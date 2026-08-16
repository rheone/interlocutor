namespace Interlocutor.Generator.Tests;

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
    /// <returns>The resulting compilation.</returns>
    public static CSharpCompilation CreateCompilation(
        string source,
        string assemblyName = "Interlocutor.Generator.Tests.Compilation"
    )
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(
            source,
            new CSharpParseOptions(Microsoft.CodeAnalysis.CSharp.LanguageVersion.Latest)
        );
        return CSharpCompilation.Create(
            assemblyName,
            [syntaxTree],
            References,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
        );
    }

    /// <summary>
    /// Compiles <paramref name="source"/> and runs <see cref="InterlocutorSourceGenerator"/> over it, returning
    /// the generator driver's run result (generated trees and reported diagnostics) without merging the
    /// generated code back into a compilation.
    /// </summary>
    /// <param name="source">The C# source to compile and analyze.</param>
    /// <returns>The generator driver's run result.</returns>
    public static GeneratorDriverRunResult Run(string source)
    {
        var driver = CSharpGeneratorDriver.Create(new InterlocutorSourceGenerator());
        driver = (CSharpGeneratorDriver)driver.RunGenerators(CreateCompilation(source));
        return driver.GetRunResult();
    }

    /// <summary>
    /// Compiles <paramref name="source"/>, runs <see cref="InterlocutorSourceGenerator"/> over it, and returns
    /// the resulting compilation with the generated sources merged in — for asserting that the emitted code
    /// itself compiles cleanly alongside the source it was generated from.
    /// </summary>
    /// <param name="source">The C# source to compile and analyze.</param>
    /// <param name="generatorDiagnostics">Diagnostics the generator itself reported (not compiler diagnostics).</param>
    /// <returns>The output compilation with the generator's emitted sources merged in.</returns>
    public static Compilation RunAndUpdate(string source, out ImmutableArray<Diagnostic> generatorDiagnostics)
    {
        var driver = CSharpGeneratorDriver.Create(new InterlocutorSourceGenerator());
        _ = driver.RunGeneratorsAndUpdateCompilation(
            CreateCompilation(source),
            out var outputCompilation,
            out generatorDiagnostics
        );
        return outputCompilation;
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
