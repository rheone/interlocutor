namespace Interlocutor.Generator.Tests.Emission;

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

        public sealed class GetFooQuery : IQuery<Outcome>;

        public sealed class GetFooQueryHandler(ISomeDependency dependency) : IQueryHandler<GetFooQuery, Outcome>
        {
            public Task<Outcome> HandleAsync(GetFooQuery query, CancellationToken ct) => throw new System.NotImplementedException();
        }
        """;

    private const string SingleBareCommandHandlerSource = """
        namespace TestApp;

        using System.Threading;
        using System.Threading.Tasks;
        using Interlocutor.Abstractions.Commands;
        using Interlocutor.CommonKernel;

        public sealed class DeleteFooCommand : ICommand;

        public sealed class DeleteFooCommandHandler : IBareCommandHandler<DeleteFooCommand, Outcome>
        {
            public Task<Outcome> HandleAsync(DeleteFooCommand command, CancellationToken ct) => throw new System.NotImplementedException();
        }
        """;

    [Fact]
    public void HandlerDiscovery_QueryHandler_ProducesNoDiagnostics_Test()
    {
        // Arrange
        // (no separate setup; uses class fixtures or direct act/assert)
        // Act
        var result = GeneratorTestHarness.Run(SingleQueryHandlerSource);

        // Assert
        Assert.Empty(result.Diagnostics);
        Assert.Single(result.Results);
    }

    [Fact]
    public void RegistrationModule_HandlerDiscovered_EmitsTypeofOnly_Test()
    {
        // Arrange
        // (no separate setup; uses class fixtures or direct act/assert)
        // Act
        var result = GeneratorTestHarness.Run(SingleQueryHandlerSource);

        // Assert
        var registrationsSource = GetGeneratedSource(result, "InterlocutorGeneratedRegistrations.g.cs");

        Assert.Contains(
            "namespace Interlocutor.Generated.Interlocutor_Generator_Tests_Compilation",
            registrationsSource,
            StringComparison.Ordinal
        );
        Assert.Contains(
            "public static class InterlocutorGeneratedRegistrations",
            registrationsSource,
            StringComparison.Ordinal
        );
        Assert.Contains(
            "registrations.Add(new global::Interlocutor.DependencyInjection.Abstractions.InterlocutorRegistration(typeof(global::Interlocutor.Abstractions.Queries.IQueryHandler<global::TestApp.GetFooQuery, global::Interlocutor.CommonKernel.Outcome>), typeof(global::TestApp.GetFooQueryHandler), global::Interlocutor.DependencyInjection.Abstractions.RegistrationLifetime.Scoped, global::Interlocutor.DependencyInjection.Abstractions.RegistrationKind.Handler));",
            registrationsSource,
            StringComparison.Ordinal
        );
        var attributeSource = GetGeneratedSource(result, "InterlocutorGeneratedModuleAttribute.g.cs");
        Assert.Contains(
            "[assembly: global::Interlocutor.DependencyInjection.Abstractions.InterlocutorGeneratedModuleAttribute(typeof(global::Interlocutor.Generated.Interlocutor_Generator_Tests_Compilation.InterlocutorGeneratedRegistrations))]",
            attributeSource,
            StringComparison.Ordinal
        );
        Assert.Contains(
            "[assembly: global::Interlocutor.DependencyInjection.Abstractions.InterlocutorGeneratedHandlerAttribute(typeof(global::TestApp.GetFooQuery), typeof(global::Interlocutor.CommonKernel.Outcome), global::Interlocutor.DependencyInjection.Abstractions.GeneratedHandlerKind.Query)]",
            attributeSource,
            StringComparison.Ordinal
        );

        // Not a single Type.GetType(string)/reflection-scan call anywhere - every reference is a compile-time
        // typeof() token, which is what keeps this emitted code AOT- and trim-friendly (ADR-0008).
        Assert.DoesNotContain("Type.GetType", registrationsSource, StringComparison.Ordinal);
        Assert.DoesNotContain("Assembly.Load", registrationsSource, StringComparison.Ordinal);
    }

    [Fact]
    public void DispatchWrapper_HandlerResolution_ResolvesFromContainer_Test()
    {
        // Arrange
        // (no separate setup; uses class fixtures or direct act/assert)
        // Act
        var result = GeneratorTestHarness.Run(SingleQueryHandlerSource);

        // Assert
        var dispatchSource = GetGeneratedSource(result, "InterlocutorGeneratedDispatch.g.cs");

        // Hard constraint (ADR-0008 / ticket 10): the generator emits registrations and dispatch, never direct
        // construction - the handler is always resolved from the container, never `new`-ed.
        Assert.DoesNotContain("new global::TestApp.GetFooQueryHandler", dispatchSource, StringComparison.Ordinal);
        Assert.DoesNotContain("Activator.CreateInstance", dispatchSource, StringComparison.Ordinal);
        Assert.DoesNotContain("MakeGenericType", dispatchSource, StringComparison.Ordinal);
        Assert.Contains(
            "services.GetService(typeof(global::Interlocutor.Abstractions.Queries.IQueryHandler<global::TestApp.GetFooQuery, global::Interlocutor.CommonKernel.Outcome>))",
            dispatchSource,
            StringComparison.Ordinal
        );

        // The behavior chain itself is no longer resolved by this emitted wrapper directly (ticket 01) - it
        // hands the resolved handler's bound call to the shared PipelineChainExecutor.ExecuteAsync, which
        // resolves PipelineChainResolver.ResolveChain internally on both dispatch paths.
        Assert.Contains(
            "global::Interlocutor.Abstractions.Pipeline.PipelineChainExecutor.ExecuteAsync<global::TestApp.GetFooQuery, global::Interlocutor.CommonKernel.Outcome>(",
            dispatchSource,
            StringComparison.Ordinal
        );
    }

    [Fact]
    public void DispatchWrapper_BareCommandHandler_ResolvesIBareCommandHandlerAndCallsHandleAsync_Test()
    {
        // Arrange
        // (no separate setup; uses class fixtures or direct act/assert)
        // Act
        var result = GeneratorTestHarness.Run(SingleBareCommandHandlerSource);

        // Assert
        var dispatchSource = GetGeneratedSource(result, "InterlocutorGeneratedDispatch.g.cs");

        // The generated wrapper class is named after HandlerKind.BareCommand, resolves the real, current
        // IBareCommandHandler<,> service type from the container, and hands its HandleAsync member to
        // PipelineChainExecutor.ExecuteAsync as the innermost pipeline delegate - the same uniform call every
        // other handler kind's dispatch wrapper emits.
        Assert.Contains(
            "internal static class global__TestApp_DeleteFooCommandHandler_BareCommand_Dispatch",
            dispatchSource,
            StringComparison.Ordinal
        );
        Assert.Contains(
            "services.GetService(typeof(global::Interlocutor.Abstractions.Commands.IBareCommandHandler<global::TestApp.DeleteFooCommand, global::Interlocutor.CommonKernel.Outcome>))",
            dispatchSource,
            StringComparison.Ordinal
        );
        Assert.Contains("handler.HandleAsync,", dispatchSource, StringComparison.Ordinal);
        Assert.DoesNotContain("VoidCommand", dispatchSource, StringComparison.Ordinal);
    }

    [Fact]
    public void GeneratedCode_WithSourceHandler_CompilesCleanly_Test()
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

    [Fact]
    public void RegistrationModule_NoHandlers_RegistersDispatcher_Test()
    {
        // Arrange
        // No handler registrations, but the registration module is never truly empty (ticket 13): the
        // compile-time generated IGeneratedRequestDispatcher is registered unconditionally, so
        // Send(object, ...) stays reflection-free even in a project with no discovered handlers.
        const string source = """
            namespace TestApp;
            public sealed class Nothing;
            """;

        // Act
        var result = GeneratorTestHarness.Run(source);

        // Assert
        var registrationsSource = GetGeneratedSource(result, "InterlocutorGeneratedRegistrations.g.cs");
        Assert.Contains(
            "new global::Interlocutor.DependencyInjection.Abstractions.InterlocutorRegistration(typeof(global::Interlocutor.Abstractions.Core.IGeneratedRequestDispatcher), typeof(global::Interlocutor.Generated.Dispatch.GeneratedRequestDispatcher), global::Interlocutor.DependencyInjection.Abstractions.RegistrationLifetime.Singleton, global::Interlocutor.DependencyInjection.Abstractions.RegistrationKind.Dispatcher)",
            registrationsSource,
            StringComparison.Ordinal
        );
        Assert.DoesNotContain(
            "global::System.Array.Empty<global::Interlocutor.DependencyInjection.Abstractions.InterlocutorRegistration>()",
            registrationsSource,
            StringComparison.Ordinal
        );

        var attributeSource = GetGeneratedSource(result, "InterlocutorGeneratedModuleAttribute.g.cs");
        Assert.DoesNotContain("InterlocutorGeneratedHandlerAttribute", attributeSource, StringComparison.Ordinal);

        Assert.Empty(result.Diagnostics);
    }

    private static string GetGeneratedSource(GeneratorDriverRunResult result, string hintNameSuffix) =>
        result
            .Results.Single()
            .GeneratedSources.Single(s => s.HintName.EndsWith(hintNameSuffix, StringComparison.Ordinal))
            .SourceText.ToString();
}
