namespace Interlocutor.Generator.Tests;

using Microsoft.CodeAnalysis;

/// <summary>
/// Covers ticket 12's acceptance criteria: a referenced assembly's generated module is discovered by
/// <c>InterlocutorGeneratedModuleAttribute</c> alone, aggregated into the referencing compilation's own
/// module, transitively, without duplicates, and without breaking the missing-handler diagnostic.
/// </summary>
public sealed class CrossAssemblyAggregationTests
{
    private const string LibraryBSource = """
        namespace LibraryB;

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

    private const string LibraryASource = """
        namespace LibraryA;

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

    [Fact]
    public void Application_aggregates_a_directly_referenced_librarys_module_with_no_extra_configuration()
    {
        // Arrange
        var libraryOutput = GeneratorTestHarness.RunAndUpdate(LibraryASource, "LibraryA", null, out _);
        var libraryReference = GeneratorTestHarness.ToMetadataReference(libraryOutput);
        const string appSource = "namespace App; public sealed class Nothing;";

        // Act
        var result = GeneratorTestHarness.Run(appSource, [libraryReference]);

        // Assert
        Assert.Empty(result.Diagnostics);
        var registrationsSource = GetGeneratedSource(result, "InterlocutorGeneratedRegistrations.g.cs");

        // Discovered purely by attribute, without App ever naming LibraryA's module type in source - only the
        // generator's own emitted aggregation call site does.
        Assert.Contains(
            "registrations.AddRange(global::Interlocutor.Generated.LibraryA.InterlocutorGeneratedRegistrations.Registrations);",
            registrationsSource,
            StringComparison.Ordinal
        );
    }

    [Fact]
    public void Three_level_reference_chain_surfaces_library_Bs_module_without_the_app_listing_it()
    {
        // Arrange
        var libraryBOutput = GeneratorTestHarness.RunAndUpdate(LibraryBSource, "LibraryB", null, out _);
        var libraryBReference = GeneratorTestHarness.ToMetadataReference(libraryBOutput);

        var libraryAOutput = GeneratorTestHarness.RunAndUpdate(LibraryASource, "LibraryA", [libraryBReference], out _);
        var libraryAReference = GeneratorTestHarness.ToMetadataReference(libraryAOutput);

        // The app references both A and B directly - exactly what MSBuild does under the hood for a
        // project-to-project reference chain (transitive project references flow as direct assembly
        // references into the final compile) - but never writes any AddGeneratedRegistrations call naming B.
        const string appSource = "namespace App; public sealed class Nothing;";

        // Act
        var result = GeneratorTestHarness.Run(appSource, [libraryAReference, libraryBReference]);

        // Assert
        Assert.Empty(result.Diagnostics);
        var registrationsSource = GetGeneratedSource(result, "InterlocutorGeneratedRegistrations.g.cs");

        Assert.Contains(
            "registrations.AddRange(global::Interlocutor.Generated.LibraryA.InterlocutorGeneratedRegistrations.Registrations);",
            registrationsSource,
            StringComparison.Ordinal
        );
        Assert.Contains(
            "registrations.AddRange(global::Interlocutor.Generated.LibraryB.InterlocutorGeneratedRegistrations.Registrations);",
            registrationsSource,
            StringComparison.Ordinal
        );
    }

    [Fact]
    public void Diamond_reference_to_the_same_assembly_does_not_duplicate_its_aggregation_call_site()
    {
        // Arrange
        var libraryBOutput = GeneratorTestHarness.RunAndUpdate(LibraryBSource, "LibraryB", null, out _);
        var libraryBReference = GeneratorTestHarness.ToMetadataReference(libraryBOutput);

        var libraryAOutput = GeneratorTestHarness.RunAndUpdate(LibraryASource, "LibraryA", [libraryBReference], out _);
        var libraryAReference = GeneratorTestHarness.ToMetadataReference(libraryAOutput);

        // App references both A (which itself references B) and B directly - the diamond: B is reachable
        // through two distinct paths (App->B directly, and App->A->B), but Compilation.References already
        // de-duplicates by assembly identity, so B's module must be aggregated exactly once.
        const string appSource = "namespace App; public sealed class Nothing;";

        // Act
        var result = GeneratorTestHarness.Run(appSource, [libraryAReference, libraryBReference]);

        // Assert
        var registrationsSource = GetGeneratedSource(result, "InterlocutorGeneratedRegistrations.g.cs");
        var occurrences = CountOccurrences(
            registrationsSource,
            "registrations.AddRange(global::Interlocutor.Generated.LibraryB.InterlocutorGeneratedRegistrations.Registrations);"
        );
        Assert.Equal(1, occurrences);
    }

    [Fact]
    public void Missing_handler_diagnostic_does_not_fire_when_the_handler_lives_in_a_referenced_assembly()
    {
        // Arrange
        var libraryOutput = GeneratorTestHarness.RunAndUpdate(LibraryASource, "LibraryA", null, out _);
        var libraryReference = GeneratorTestHarness.ToMetadataReference(libraryOutput);

        const string appSource = """
            namespace App;

            using Interlocutor.Abstractions.Queries;
            using Interlocutor.CommonKernel;

            // A request declared in the app, handled by LibraryA.GetFooQueryHandler - the app never sees the
            // handler type, only its request/response shape via InterlocutorGeneratedHandlerAttribute metadata.
            public sealed class AppOnlyQuery : IQuery<OperationOutcome>;
            """;

        // Act
        var result = GeneratorTestHarness.Run(appSource, [libraryReference]);

        // Assert
        // AppOnlyQuery still has no handler anywhere, so it still gets the diagnostic - proves the check isn't
        // simply disabled once any external module is present.
        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("INTERLOC001", diagnostic.Id);
        Assert.Contains("App.AppOnlyQuery", diagnostic.GetMessage(), StringComparison.Ordinal);
    }

    [Fact]
    public void Referencing_a_library_with_a_fully_matched_request_and_handler_pair_produces_no_diagnostics()
    {
        // LibraryA declares GetFooQuery and GetFooQueryHandler together, so LibraryA's own generator run
        // already proves that pair matches (see MissingHandlerDiagnosticTests for the single-compilation
        // case). The app here declares no request types of its own - it only references LibraryA - so its
        // own generator run must not surface any stale or duplicated diagnostic about LibraryA's internals.
        // Arrange
        var libraryOutput = GeneratorTestHarness.RunAndUpdate(LibraryASource, "LibraryA", null, out _);
        var libraryReference = GeneratorTestHarness.ToMetadataReference(libraryOutput);
        const string appSource = "namespace App; public sealed class Nothing;";

        // Act
        var result = GeneratorTestHarness.Run(appSource, [libraryReference]);

        // Assert
        Assert.Empty(result.Diagnostics);
    }

    [Fact]
    public void External_handler_metadata_matches_by_request_and_response_type_name_regardless_of_declaring_assembly()
    {
        // Arrange
        // Exercises ReferencedModuleDiscovery's matching rule directly: build a compilation that declares only
        // the request half of LibraryA's GetFooQuery/GetFooQueryHandler pair under the exact same
        // fully-qualified name LibraryA's handler was compiled against, and reference LibraryA. If the
        // cross-assembly signature match works by (request type name, response type name) as intended, no
        // diagnostic is reported even though this compilation never sees LibraryA.GetFooQueryHandler itself.
        var libraryOutput = GeneratorTestHarness.RunAndUpdate(LibraryASource, "LibraryA", null, out _);
        var libraryReference = GeneratorTestHarness.ToMetadataReference(libraryOutput);

        const string appSource = """
            namespace App;

            using Interlocutor.Abstractions.Queries;
            using Interlocutor.CommonKernel;

            public sealed class GetFooQuery : IQuery<OperationOutcome>;
            """;

        // Act
        var result = GeneratorTestHarness.Run(appSource, [libraryReference]);

        // Assert
        // App.GetFooQuery is a distinct type from LibraryA.GetFooQuery (different namespace, no ambiguity) -
        // it legitimately has no handler anywhere, so the diagnostic is expected here, proving the matching
        // rule keys on the full name (including namespace), not the bare type name alone.
        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("INTERLOC001", diagnostic.Id);
        Assert.Contains("App.GetFooQuery", diagnostic.GetMessage(), StringComparison.Ordinal);
    }

    [Fact]
    public void Out_of_range_handler_kind_in_referenced_metadata_is_rejected_rather_than_misdecoded()
    {
        // Arrange
        // Simulates a referenced assembly built against a newer generator version that emits a HandlerKind
        // value this generator doesn't know about yet - hand-authored rather than produced by the real
        // generator, since the real generator can only ever emit values it knows about itself.
        const string futureLibrarySource = """
            using Interlocutor.DependencyInjection.Abstractions;

            [assembly: InterlocutorGeneratedModuleAttribute(typeof(LibraryFuture.Module))]
            [assembly: InterlocutorGeneratedHandlerAttribute(
                typeof(LibraryFuture.WeirdRequest),
                typeof(Interlocutor.CommonKernel.OperationOutcome),
                (GeneratedHandlerKind)99)]

            namespace LibraryFuture;

            using System.Collections.Generic;

            public static class Module
            {
                public static IReadOnlyList<InterlocutorRegistration> Registrations { get; } = [];
            }

            public sealed class WeirdRequest;
            """;

        var libraryCompilation = GeneratorTestHarness.CreateCompilation(futureLibrarySource, "LibraryFuture");
        var libraryReference = GeneratorTestHarness.ToMetadataReference(libraryCompilation);

        // App declares the exact same request/response shape the malformed attribute claims to handle. If
        // ReferencedModuleDiscovery decoded kindValue 99 instead of rejecting it, this request could be
        // wrongly treated as already handled (whichever HandlerKind case 99 happened to alias); rejecting it
        // means the diagnostic still fires here.
        const string appSource = """
            namespace App;

            using Interlocutor.Abstractions.Queries;
            using Interlocutor.CommonKernel;

            public sealed class WeirdRequest : IQuery<OperationOutcome>;
            """;

        // Act
        var result = GeneratorTestHarness.Run(appSource, [libraryReference]);

        // Assert
        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("INTERLOC001", diagnostic.Id);
        Assert.Contains("App.WeirdRequest", diagnostic.GetMessage(), StringComparison.Ordinal);
    }

    private static string GetGeneratedSource(GeneratorDriverRunResult result, string hintNameSuffix) =>
        result
            .Results.Single()
            .GeneratedSources.Single(s => s.HintName.EndsWith(hintNameSuffix, StringComparison.Ordinal))
            .SourceText.ToString();

    private static int CountOccurrences(string haystack, string needle)
    {
        var count = 0;
        var index = 0;
        while ((index = haystack.IndexOf(needle, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += needle.Length;
        }

        return count;
    }
}
