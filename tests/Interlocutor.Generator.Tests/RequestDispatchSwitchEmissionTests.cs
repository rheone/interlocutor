namespace Interlocutor.Generator.Tests;

using Microsoft.CodeAnalysis;

/// <summary>
/// Snapshot tests for <see cref="InterlocutorSourceGenerator"/>'s emitted <c>GeneratedRequestDispatcher</c> -
/// the generated type switch replacing reflection-based runtime-typed dispatch (ticket 13) - covering the
/// zero-handler and single-handler cases the acceptance criteria call out explicitly, plus the
/// unregistered-request and unrecognized-type exception cases.
/// </summary>
public sealed class RequestDispatchSwitchEmissionTests
{
    private const string SingleQueryHandlerSource = """
        namespace TestApp;

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
    public void Zero_handlers_emits_a_dispatcher_whose_switch_only_has_a_default_branch()
    {
        const string source = """
            namespace TestApp;
            public sealed class Nothing;
            """;

        var result = GeneratorTestHarness.Run(source);

        var dispatchSwitchSource = GetGeneratedSource(result, "InterlocutorGeneratedDispatchSwitch.g.cs");

        Assert.Contains(
            "internal sealed class GeneratedRequestDispatcher : global::Interlocutor.Abstractions.Core.IGeneratedRequestDispatcher",
            dispatchSwitchSource,
            StringComparison.Ordinal
        );
        Assert.DoesNotContain("case ", dispatchSwitchSource, StringComparison.Ordinal);
        Assert.Contains(
            "throw new global::Interlocutor.Abstractions.Core.UnsupportedRequestTypeException(request.GetType());",
            dispatchSwitchSource,
            StringComparison.Ordinal
        );
        Assert.Empty(result.Diagnostics);
    }

    [Fact]
    public void Single_handler_emits_one_case_dispatching_into_its_wrapper()
    {
        var result = GeneratorTestHarness.Run(SingleQueryHandlerSource);

        var dispatchSwitchSource = GetGeneratedSource(result, "InterlocutorGeneratedDispatchSwitch.g.cs");

        Assert.Contains("case global::TestApp.GetFooQuery typedRequest:", dispatchSwitchSource, StringComparison.Ordinal);
        Assert.Contains(
            "return await global::Interlocutor.Generated.Dispatch.global__TestApp_GetFooQueryHandler_Query_Dispatch.HandleAsync(typedRequest, services, ct).ConfigureAwait(false);",
            dispatchSwitchSource,
            StringComparison.Ordinal
        );

        // Still falls through to the same default branch for any type outside the switch.
        Assert.Contains(
            "throw new global::Interlocutor.Abstractions.Core.UnsupportedRequestTypeException(request.GetType());",
            dispatchSwitchSource,
            StringComparison.Ordinal
        );
    }

    [Fact]
    public void Request_with_no_matching_handler_emits_a_case_throwing_HandlerNotRegisteredException()
    {
        const string source = """
            namespace TestApp;

            using Interlocutor.Abstractions.Queries;
            using Interlocutor.CommonKernel;

            public sealed class GetFooQuery : IQuery<OperationOutcome>;
            """;

        var result = GeneratorTestHarness.Run(source);

        var dispatchSwitchSource = GetGeneratedSource(result, "InterlocutorGeneratedDispatchSwitch.g.cs");

        Assert.Contains("case global::TestApp.GetFooQuery:", dispatchSwitchSource, StringComparison.Ordinal);
        Assert.Contains(
            "throw new global::Interlocutor.Abstractions.Core.HandlerNotRegisteredException(typeof(global::TestApp.GetFooQuery), "
                + "typeof(global::Interlocutor.Abstractions.Queries.IQueryHandler<global::TestApp.GetFooQuery, global::Interlocutor.CommonKernel.OperationOutcome>));",
            dispatchSwitchSource,
            StringComparison.Ordinal
        );

        // A missing handler is a build-time warning (INTERLOC001), not a generator failure - the dispatcher
        // still emits a valid, compilable case for it.
        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("INTERLOC001", diagnostic.Id);
    }

    [Fact]
    public void Unregistered_void_command_emits_a_case_throwing_HandlerNotRegisteredException_without_a_handler_type()
    {
        const string source = """
            namespace TestApp;

            using Interlocutor.Abstractions.Commands;

            public sealed class DeleteFooCommand : ICommand;
            """;

        var result = GeneratorTestHarness.Run(source);

        var dispatchSwitchSource = GetGeneratedSource(result, "InterlocutorGeneratedDispatchSwitch.g.cs");

        Assert.Contains("case global::TestApp.DeleteFooCommand:", dispatchSwitchSource, StringComparison.Ordinal);

        // A unit command's result type is caller-chosen, not declared on the command - no single closed
        // handler interface to name, mirroring the reflection path's null handlerType for this same case.
        Assert.Contains(
            "throw new global::Interlocutor.Abstractions.Core.HandlerNotRegisteredException(typeof(global::TestApp.DeleteFooCommand));",
            dispatchSwitchSource,
            StringComparison.Ordinal
        );
    }

    [Fact]
    public void Void_command_with_a_handler_emits_a_dispatch_case()
    {
        const string source = """
            namespace TestApp;

            using System.Threading;
            using System.Threading.Tasks;
            using Interlocutor.Abstractions.Commands;
            using Interlocutor.CommonKernel;

            public sealed class DeleteFooCommand : ICommand;

            public sealed class DeleteFooCommandHandler : IVoidCommandHandler<DeleteFooCommand, OperationOutcome>
            {
                public Task<OperationOutcome> Handle(DeleteFooCommand command, CancellationToken ct) => throw new System.NotImplementedException();
            }
            """;

        var result = GeneratorTestHarness.Run(source);

        var dispatchSwitchSource = GetGeneratedSource(result, "InterlocutorGeneratedDispatchSwitch.g.cs");

        Assert.Contains("case global::TestApp.DeleteFooCommand typedRequest:", dispatchSwitchSource, StringComparison.Ordinal);
        Assert.DoesNotContain("HandlerNotRegisteredException", dispatchSwitchSource, StringComparison.Ordinal);
    }

    [Fact]
    public void Dispatch_switch_never_uses_reflection_apis()
    {
        var result = GeneratorTestHarness.Run(SingleQueryHandlerSource);

        var dispatchSwitchSource = GetGeneratedSource(result, "InterlocutorGeneratedDispatchSwitch.g.cs");

        Assert.DoesNotContain("Activator.CreateInstance", dispatchSwitchSource, StringComparison.Ordinal);
        Assert.DoesNotContain("MakeGenericType", dispatchSwitchSource, StringComparison.Ordinal);
        Assert.DoesNotContain("MakeGenericMethod", dispatchSwitchSource, StringComparison.Ordinal);
        Assert.DoesNotContain("Type.GetType", dispatchSwitchSource, StringComparison.Ordinal);
        Assert.DoesNotContain("GetTypes()", dispatchSwitchSource, StringComparison.Ordinal);
    }

    [Fact]
    public void Emitted_dispatch_switch_compiles_cleanly_alongside_the_source_it_was_generated_from()
    {
        var outputCompilation = GeneratorTestHarness.RunAndUpdate(SingleQueryHandlerSource, out var generatorDiagnostics);

        Assert.Empty(generatorDiagnostics);

        var errors = outputCompilation
            .GetDiagnostics(TestContext.Current.CancellationToken)
            .Where(d => d.Severity == DiagnosticSeverity.Error)
            .ToList();
        Assert.True(errors.Count == 0, string.Join(Environment.NewLine, errors.Select(e => e.ToString())));
    }

    private static string GetGeneratedSource(GeneratorDriverRunResult result, string hintNameSuffix) =>
        result
            .Results.Single()
            .GeneratedSources.Single(s => s.HintName.EndsWith(hintNameSuffix, StringComparison.Ordinal))
            .SourceText.ToString();
}
