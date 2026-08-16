namespace Interlocutor.Generator.Tests;

using Microsoft.CodeAnalysis;

/// <summary>
/// Snapshot tests for <see cref="InterlocutorSourceGenerator"/>'s emitted registration module and dispatch
/// wrappers, and for the hard constraint that neither ever directly constructs a handler.
/// </summary>
public sealed class HandlerDiscoveryEmissionTests
{
    private const string SingleQueryHandlerSource = """
        namespace TestApp;

        using System.Threading;
        using System.Threading.Tasks;
        using Interlocutor.Abstractions.Queries;
        using Interlocutor.CommonKernel;

        public interface ISomeDependency;

        public sealed class GetFooQuery : IQuery<OperationOutcome>;

        public sealed class GetFooQueryHandler(ISomeDependency dependency) : IQueryHandler<GetFooQuery, OperationOutcome>
        {
            public Task<OperationOutcome> Handle(GetFooQuery query, CancellationToken ct) => throw new System.NotImplementedException();
        }
        """;

    [Fact]
    public void Discovers_query_handler_and_produces_no_diagnostics()
    {
        var result = GeneratorTestHarness.Run(SingleQueryHandlerSource);

        Assert.Empty(result.Diagnostics);
        Assert.Single(result.Results);
    }

    [Fact]
    public void Registration_module_emits_a_registration_built_from_typeof_only()
    {
        var result = GeneratorTestHarness.Run(SingleQueryHandlerSource);

        var registrationsSource = GetGeneratedSource(result, "InterlocutorGeneratedRegistrations.g.cs");

        Assert.Contains(
            "public static class InterlocutorGeneratedRegistrations",
            registrationsSource,
            StringComparison.Ordinal
        );
        Assert.Contains(
            "new global::Interlocutor.DependencyInjection.Abstractions.InterlocutorRegistration(typeof(global::Interlocutor.Abstractions.Queries.IQueryHandler<global::TestApp.GetFooQuery, global::Interlocutor.CommonKernel.OperationOutcome>), typeof(global::TestApp.GetFooQueryHandler), global::Interlocutor.DependencyInjection.Abstractions.RegistrationLifetime.Scoped, global::Interlocutor.DependencyInjection.Abstractions.RegistrationKind.Handler)",
            registrationsSource,
            StringComparison.Ordinal
        );
        var attributeSource = GetGeneratedSource(result, "InterlocutorGeneratedModuleAttribute.g.cs");
        Assert.Contains(
            "[assembly: global::Interlocutor.DependencyInjection.Abstractions.InterlocutorGeneratedModuleAttribute(typeof(global::Interlocutor.Generated.InterlocutorGeneratedRegistrations))]",
            attributeSource,
            StringComparison.Ordinal
        );

        // Not a single Type.GetType(string)/reflection-scan call anywhere - every reference is a compile-time
        // typeof() token, which is what keeps this emitted code AOT- and trim-friendly (ADR-0008).
        Assert.DoesNotContain("Type.GetType", registrationsSource, StringComparison.Ordinal);
        Assert.DoesNotContain("Assembly.Load", registrationsSource, StringComparison.Ordinal);
    }

    [Fact]
    public void Dispatch_wrapper_resolves_handler_from_the_container_and_never_constructs_it_directly()
    {
        var result = GeneratorTestHarness.Run(SingleQueryHandlerSource);

        var dispatchSource = GetGeneratedSource(result, "InterlocutorGeneratedDispatch.g.cs");

        // Hard constraint (ADR-0008 / ticket 10): the generator emits registrations and dispatch, never direct
        // construction - the handler is always resolved from the container, never `new`-ed.
        Assert.DoesNotContain("new global::TestApp.GetFooQueryHandler", dispatchSource, StringComparison.Ordinal);
        Assert.DoesNotContain("Activator.CreateInstance", dispatchSource, StringComparison.Ordinal);
        Assert.DoesNotContain("MakeGenericType", dispatchSource, StringComparison.Ordinal);
        Assert.Contains(
            "services.GetService(typeof(global::Interlocutor.Abstractions.Queries.IQueryHandler<global::TestApp.GetFooQuery, global::Interlocutor.CommonKernel.OperationOutcome>))",
            dispatchSource,
            StringComparison.Ordinal
        );
        Assert.Contains(
            "services.GetService(typeof(global::System.Collections.Generic.IEnumerable<global::Interlocutor.Abstractions.Pipeline.IPipelineBehavior<global::TestApp.GetFooQuery, global::Interlocutor.CommonKernel.OperationOutcome>>))",
            dispatchSource,
            StringComparison.Ordinal
        );
    }

    [Fact]
    public void Emitted_code_compiles_cleanly_alongside_the_source_it_was_generated_from()
    {
        var outputCompilation = GeneratorTestHarness.RunAndUpdate(SingleQueryHandlerSource, out var generatorDiagnostics);

        Assert.Empty(generatorDiagnostics);

        var errors = outputCompilation
            .GetDiagnostics(TestContext.Current.CancellationToken)
            .Where(d => d.Severity == DiagnosticSeverity.Error)
            .ToList();
        Assert.True(errors.Count == 0, string.Join(Environment.NewLine, errors.Select(e => e.ToString())));
    }

    [Fact]
    public void No_handlers_emits_an_empty_but_valid_registration_module()
    {
        const string source = """
            namespace TestApp;
            public sealed class Nothing;
            """;

        var result = GeneratorTestHarness.Run(source);

        var registrationsSource = GetGeneratedSource(result, "InterlocutorGeneratedRegistrations.g.cs");
        Assert.Contains(
            "global::System.Array.Empty<global::Interlocutor.DependencyInjection.Abstractions.InterlocutorRegistration>()",
            registrationsSource,
            StringComparison.Ordinal
        );
        Assert.Empty(result.Diagnostics);
    }

    private static string GetGeneratedSource(GeneratorDriverRunResult result, string hintNameSuffix) =>
        result
            .Results.Single()
            .GeneratedSources.Single(s => s.HintName.EndsWith(hintNameSuffix, StringComparison.Ordinal))
            .SourceText.ToString();
}
