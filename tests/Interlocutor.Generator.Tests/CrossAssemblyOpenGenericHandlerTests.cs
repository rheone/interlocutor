namespace Interlocutor.Generator.Tests;

using Interlocutor.Generator.Analysis;
using Interlocutor.Generator.Diagnostics;
using Microsoft.CodeAnalysis;

/// <summary>
/// Covers ticket 15: open generic handler closing (ticket 11) is same-compilation-only -
/// <see cref="OpenGenericHandlerResolver"/> only ever sees open generic handlers and closed usages discovered
/// by its own compilation's syntax trees, never a referenced library's. An open generic handler defined in
/// one assembly, used only via a closed instantiation in a different assembly that references it, previously
/// closed silently to nothing - no diagnostic, a runtime <c>HandlerNotRegisteredException</c> at first
/// dispatch instead. <see cref="InterlocutorDiagnostics.UnresolvedGenericRequestUsage"/> (INTERLOC008) now
/// surfaces the same gap at build time.
/// </summary>
public sealed class CrossAssemblyOpenGenericHandlerTests
{
    private const string LibrarySource = """
        namespace LibraryA;

        using System.Threading;
        using System.Threading.Tasks;
        using Interlocutor.Abstractions.Queries;
        using Interlocutor.CommonKernel;

        public interface IEntity;

        public sealed class GenericQuery<T> : IQuery<Outcome<T, string>> where T : IEntity;

        public sealed class GenericQueryHandler<T> : IQueryHandler<GenericQuery<T>, Outcome<T, string>>
            where T : IEntity
        {
            public Task<Outcome<T, string>> HandleAsync(GenericQuery<T> query, CancellationToken ct) =>
                throw new System.NotImplementedException();
        }
        """;

    private const string AppSourceUsingLibraryHandlerOnly = """
        namespace App;

        using LibraryA;

        public sealed class Foo : IEntity;

        public sealed class UsesGenericQuery
        {
            public void Use()
            {
                var query = new GenericQuery<Foo>();
            }
        }
        """;

    [Fact]
    public void ClosedUsageInApplication_OfOpenHandlerDeclaredOnlyInReferencedLibrary_ReportsUnresolvedGenericRequestUsage_Test()
    {
        // Arrange
        // LibraryA never itself uses GenericQuery<T> with a concrete type argument, so its own generator run
        // closes nothing and emits no handler attribute for App's ReferencedModuleDiscovery to find - the
        // exact silent gap this ticket closes: GenericQueryHandler<T> is real, but invisible to App's own
        // OpenGenericHandlerResolver pass, which only ever sees App's own syntax.
        var libraryOutput = GeneratorTestHarness.RunAndUpdate(LibrarySource, "LibraryA", null, out var libraryDiagnostics);
        Assert.Empty(libraryDiagnostics);
        var libraryReference = GeneratorTestHarness.ToMetadataReference(libraryOutput);

        // Act
        var result = GeneratorTestHarness.Run(AppSourceUsingLibraryHandlerOnly, [libraryReference]);

        // Assert
        var diagnostic = Assert.Single(result.Diagnostics, d => d.Id == "INTERLOC008");
        Assert.Contains("GenericQuery<global::App.Foo>", diagnostic.GetMessage(), StringComparison.Ordinal);
    }

    [Fact]
    public void ClosedUsageInSameCompilationAsOpenHandler_ReportsNoUnresolvedGenericRequestUsageDiagnostic_Test()
    {
        // Arrange
        // Regression check: the working same-compilation case (ticket 11) must not start reporting
        // INTERLOC008 once a usage is actually resolved locally.
        // Act
        var result = GeneratorTestHarness.Run(
            LibrarySource.Replace(
                "public sealed class GenericQueryHandler<T>",
                """
                public sealed class Foo : IEntity;

                public sealed class UsesGenericQuery
                {
                    public void Use()
                    {
                        var query = new GenericQuery<Foo>();
                    }
                }

                public sealed class GenericQueryHandler<T>
                """,
                StringComparison.Ordinal
            )
        );

        // Assert
        Assert.DoesNotContain(result.Diagnostics, d => d.Id == "INTERLOC008");
    }
}
