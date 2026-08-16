namespace Interlocutor.Generator.Tests;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

/// <summary>
/// Tests for <see cref="InterlocutorDiagnostics.MissingHandler"/>: its identifier, default severity, reported
/// location, and the standard <c>.editorconfig</c> escalation mechanism.
/// </summary>
public sealed class MissingHandlerDiagnosticTests
{
    [Fact]
    public void Query_with_no_handler_reports_missing_handler_diagnostic()
    {
        const string source = """
            namespace TestApp;

            using Interlocutor.Abstractions.Queries;
            using Interlocutor.CommonKernel;

            public sealed class GetFooQuery : IQuery<OperationOutcome>;
            """;

        var result = GeneratorTestHarness.Run(source);

        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("INTERLOC001", diagnostic.Id);
        Assert.Equal(DiagnosticSeverity.Warning, diagnostic.Severity);
        Assert.Contains("TestApp.GetFooQuery", diagnostic.GetMessage(), StringComparison.Ordinal);

        // Anchored at the request type's own declaration, not at the compilation as a whole.
        var lineSpan = diagnostic.Location.GetLineSpan();
        Assert.True(lineSpan.IsValid);
        Assert.Contains("GetFooQuery", source.Split('\n')[lineSpan.StartLinePosition.Line], StringComparison.Ordinal);
    }

    [Fact]
    public void Command_with_no_handler_reports_missing_handler_diagnostic()
    {
        const string source = """
            namespace TestApp;

            using Interlocutor.Abstractions.Commands;
            using Interlocutor.CommonKernel;

            public sealed class CreateFooCommand : ICommand<OperationOutcome>;
            """;

        var result = GeneratorTestHarness.Run(source);

        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("INTERLOC001", diagnostic.Id);
        Assert.Contains("TestApp.CreateFooCommand", diagnostic.GetMessage(), StringComparison.Ordinal);
    }

    [Fact]
    public void Void_command_with_no_handler_reports_missing_handler_diagnostic()
    {
        const string source = """
            namespace TestApp;

            using Interlocutor.Abstractions.Commands;

            public sealed class DeleteFooCommand : ICommand;
            """;

        var result = GeneratorTestHarness.Run(source);

        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("INTERLOC001", diagnostic.Id);
        Assert.Contains("TestApp.DeleteFooCommand", diagnostic.GetMessage(), StringComparison.Ordinal);
    }

    [Fact]
    public void Query_with_a_matching_handler_reports_no_diagnostic()
    {
        const string source = """
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

        var result = GeneratorTestHarness.Run(source);

        Assert.Empty(result.Diagnostics);
    }

    [Fact]
    public void Void_command_matches_a_handler_regardless_of_the_handlers_chosen_result_type()
    {
        const string source = """
            namespace TestApp;

            using System.Threading;
            using System.Threading.Tasks;
            using Interlocutor.Abstractions.Commands;
            using Interlocutor.CommonKernel;

            public sealed class DeleteFooCommand : ICommand;

            public sealed class DeleteFooCommandHandler : IVoidCommandHandler<DeleteFooCommand, OperationOutcome<int, string>>
            {
                public Task<OperationOutcome<int, string>> Handle(DeleteFooCommand command, CancellationToken ct) =>
                    throw new System.NotImplementedException();
            }
            """;

        var result = GeneratorTestHarness.Run(source);

        Assert.Empty(result.Diagnostics);
    }

    [Fact]
    public void Consumer_escalates_the_diagnostic_to_an_error_via_the_standard_editorconfig_mechanism()
    {
        const string source = """
            namespace TestApp;

            using Interlocutor.Abstractions.Queries;
            using Interlocutor.CommonKernel;

            public sealed class GetFooQuery : IQuery<OperationOutcome>;
            """;

        var compilation = GeneratorTestHarness.CreateCompilation(source);
        var driver = CSharpGeneratorDriver.Create(
            [new InterlocutorSourceGenerator().AsSourceGenerator()],
            driverOptions: new GeneratorDriverOptions(default, trackIncrementalGeneratorSteps: false)
        );

        // The escalation mechanism this ticket's acceptance criteria describe is the same one every other
        // diagnostic in this repository uses (see .editorconfig's `dotnet_diagnostic.<ID>.severity` entries) —
        // exercised here via CSharpCompilationOptions.SpecificDiagnosticOptions, the API-level equivalent of an
        // .editorconfig `dotnet_diagnostic.INTERLOC001.severity = error` entry.
        var escalated = compilation.WithOptions(
            compilation.Options.WithSpecificDiagnosticOptions(
                new System.Collections.Generic.Dictionary<string, ReportDiagnostic> { ["INTERLOC001"] = ReportDiagnostic.Error }
            )
        );

        _ = driver.RunGeneratorsAndUpdateCompilation(
            escalated,
            out _,
            out var generatorDiagnostics,
            TestContext.Current.CancellationToken
        );

        var reported = generatorDiagnostics.Single(d => d.Id == "INTERLOC001");
        Assert.Equal(DiagnosticSeverity.Error, reported.Severity);
    }
}
