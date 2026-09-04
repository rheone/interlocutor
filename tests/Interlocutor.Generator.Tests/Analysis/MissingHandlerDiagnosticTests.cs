namespace Interlocutor.Generator.Tests.Analysis;

using Interlocutor.Generator.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

/// <summary>
/// Tests for <see cref="InterlocutorDiagnostics.MissingHandler"/>: its identifier, default severity, reported
/// location, and the standard <c>.editorconfig</c> escalation mechanism.
/// </summary>
public sealed class MissingHandlerDiagnosticTests
{
    [Fact]
    public void MissingHandlerDiagnostic_Query_Reports_Test()
    {
        // Arrange
        const string source = """
            namespace TestApp;

            using Interlocutor.Abstractions.Queries;
            using Interlocutor.CommonKernel;

            public sealed class GetFooQuery : IQuery<Outcome>;
            """;

        // Act
        var result = GeneratorTestHarness.Run(source);

        // Assert
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
    public void MissingHandlerDiagnostic_Command_Reports_Test()
    {
        // Arrange
        const string source = """
            namespace TestApp;

            using Interlocutor.Abstractions.Commands;
            using Interlocutor.CommonKernel;

            public sealed class CreateFooCommand : ICommand<Outcome>;
            """;

        // Act
        var result = GeneratorTestHarness.Run(source);

        // Assert
        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("INTERLOC001", diagnostic.Id);
        Assert.Contains("TestApp.CreateFooCommand", diagnostic.GetMessage(), StringComparison.Ordinal);
    }

    [Fact]
    public void MissingHandlerDiagnostic_VoidCommand_Reports_Test()
    {
        // Arrange
        const string source = """
            namespace TestApp;

            using Interlocutor.Abstractions.Commands;

            public sealed class DeleteFooCommand : ICommand;
            """;

        // Act
        var result = GeneratorTestHarness.Run(source);

        // Assert
        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("INTERLOC001", diagnostic.Id);
        Assert.Contains("TestApp.DeleteFooCommand", diagnostic.GetMessage(), StringComparison.Ordinal);
    }

    [Fact]
    public void HandlerMatching_Query_ProducesNoDiagnostic_Test()
    {
        // Arrange
        const string source = """
            namespace TestApp;

            using System.Threading;
            using System.Threading.Tasks;
            using Interlocutor.Abstractions.Queries;
            using Interlocutor.CommonKernel;

            public sealed class GetFooQuery : IQuery<Outcome>;

            public sealed class GetFooQueryHandler : IQueryHandler<GetFooQuery, Outcome>
            {
                public Task<Outcome> Handle(GetFooQuery query, CancellationToken ct) => throw new System.NotImplementedException();
            }
            """;

        // Act
        var result = GeneratorTestHarness.Run(source);

        // Assert
        Assert.Empty(result.Diagnostics);
    }

    [Fact]
    public void HandlerMatching_VoidCommandAnyResult_Matches_Test()
    {
        // Arrange
        const string source = """
            namespace TestApp;

            using System.Threading;
            using System.Threading.Tasks;
            using Interlocutor.Abstractions.Commands;
            using Interlocutor.CommonKernel;

            public sealed class DeleteFooCommand : ICommand;

            public sealed class DeleteFooCommandHandler : IVoidCommandHandler<DeleteFooCommand, Outcome<int, string>>
            {
                public Task<Outcome<int, string>> Handle(DeleteFooCommand command, CancellationToken ct) =>
                    throw new System.NotImplementedException();
            }
            """;

        // Act
        var result = GeneratorTestHarness.Run(source);

        // Assert
        Assert.Empty(result.Diagnostics);
    }

    [Fact]
    public void Notification_NoHandlers_ProducesNoDiagnostic_Test()
    {
        // Unlike a query or command, a notification is valid with zero registered handlers - INotification is
        // not tracked as a request marker by the generator at all, so it never enters the missing-handler check.
        // Arrange
        const string source = """
            namespace TestApp;

            using Interlocutor.Abstractions.Notifications;

            public sealed class FooHappened : INotification;
            """;

        // Act
        var result = GeneratorTestHarness.Run(source);

        // Assert
        Assert.Empty(result.Diagnostics);
    }

    [Fact]
    public void Notification_OneHandler_ProducesNoDiagnostic_Test()
    {
        // Arrange
        const string source = """
            namespace TestApp;

            using System.Threading;
            using System.Threading.Tasks;
            using Interlocutor.Abstractions.Notifications;

            public sealed class FooHappened : INotification;

            public sealed class FooHappenedHandler : INotificationHandler<FooHappened>
            {
                public Task Handle(FooHappened notification, CancellationToken ct) => Task.CompletedTask;
            }
            """;

        // Act
        var result = GeneratorTestHarness.Run(source);

        // Assert
        Assert.Empty(result.Diagnostics);
    }

    [Fact]
    public void Notification_MultipleHandlers_ProducesNoDiagnostic_Test()
    {
        // Unlike a query or command, zero-or-many is the valid arity for a notification, so multiple concrete
        // handlers for the same notification never trigger INTERLOC002 either.
        // Arrange
        const string source = """
            namespace TestApp;

            using System.Threading;
            using System.Threading.Tasks;
            using Interlocutor.Abstractions.Notifications;

            public sealed class FooHappened : INotification;

            public sealed class FooHappenedHandlerOne : INotificationHandler<FooHappened>
            {
                public Task Handle(FooHappened notification, CancellationToken ct) => Task.CompletedTask;
            }

            public sealed class FooHappenedHandlerTwo : INotificationHandler<FooHappened>
            {
                public Task Handle(FooHappened notification, CancellationToken ct) => Task.CompletedTask;
            }
            """;

        // Act
        var result = GeneratorTestHarness.Run(source);

        // Assert
        Assert.Empty(result.Diagnostics);
    }

    [Fact]
    public void DuplicateHandlerDiagnostic_Query_ReportsNamesBoth_Test()
    {
        // Arrange
        const string source = """
            namespace TestApp;

            using System.Threading;
            using System.Threading.Tasks;
            using Interlocutor.Abstractions.Queries;
            using Interlocutor.CommonKernel;

            public sealed class GetFooQuery : IQuery<Outcome>;

            public sealed class GetFooQueryHandlerOne : IQueryHandler<GetFooQuery, Outcome>
            {
                public Task<Outcome> Handle(GetFooQuery query, CancellationToken ct) => throw new System.NotImplementedException();
            }

            public sealed class GetFooQueryHandlerTwo : IQueryHandler<GetFooQuery, Outcome>
            {
                public Task<Outcome> Handle(GetFooQuery query, CancellationToken ct) => throw new System.NotImplementedException();
            }
            """;

        // Act
        var result = GeneratorTestHarness.Run(source);

        // Assert
        Assert.Equal(2, result.Diagnostics.Length);
        Assert.All(result.Diagnostics, d => Assert.Equal("INTERLOC002", d.Id));
        Assert.All(result.Diagnostics, d => Assert.Contains("TestApp.GetFooQuery", d.GetMessage(), StringComparison.Ordinal));
        Assert.All(result.Diagnostics, d => Assert.Contains("GetFooQueryHandlerOne", d.GetMessage(), StringComparison.Ordinal));
        Assert.All(result.Diagnostics, d => Assert.Contains("GetFooQueryHandlerTwo", d.GetMessage(), StringComparison.Ordinal));
    }

    [Fact]
    public void DuplicateHandlerDiagnostic_Command_ReportsNamesBoth_Test()
    {
        // Arrange
        const string source = """
            namespace TestApp;

            using System.Threading;
            using System.Threading.Tasks;
            using Interlocutor.Abstractions.Commands;
            using Interlocutor.CommonKernel;

            public sealed class CreateFooCommand : ICommand<Outcome>;

            public sealed class CreateFooCommandHandlerOne : ICommandHandler<CreateFooCommand, Outcome>
            {
                public Task<Outcome> Handle(CreateFooCommand command, CancellationToken ct) => throw new System.NotImplementedException();
            }

            public sealed class CreateFooCommandHandlerTwo : ICommandHandler<CreateFooCommand, Outcome>
            {
                public Task<Outcome> Handle(CreateFooCommand command, CancellationToken ct) => throw new System.NotImplementedException();
            }
            """;

        // Act
        var result = GeneratorTestHarness.Run(source);

        // Assert
        Assert.Equal(2, result.Diagnostics.Length);
        Assert.All(result.Diagnostics, d => Assert.Equal("INTERLOC002", d.Id));
        Assert.All(
            result.Diagnostics,
            d => Assert.Contains("TestApp.CreateFooCommand", d.GetMessage(), StringComparison.Ordinal)
        );
        Assert.All(
            result.Diagnostics,
            d => Assert.Contains("CreateFooCommandHandlerOne", d.GetMessage(), StringComparison.Ordinal)
        );
        Assert.All(
            result.Diagnostics,
            d => Assert.Contains("CreateFooCommandHandlerTwo", d.GetMessage(), StringComparison.Ordinal)
        );
    }

    [Fact]
    public void DiagnosticEscalation_EditorConfig_EscalatesViaStandardMechanism_Test()
    {
        // Arrange
        const string source = """
            namespace TestApp;

            using Interlocutor.Abstractions.Queries;
            using Interlocutor.CommonKernel;

            public sealed class GetFooQuery : IQuery<Outcome>;
            """;

        var compilation = GeneratorTestHarness.CreateCompilation(source);
        var driver = CSharpGeneratorDriver.Create(
            [new InterlocutorSourceGenerator().AsSourceGenerator()],
            driverOptions: new GeneratorDriverOptions(default, trackIncrementalGeneratorSteps: false)
        );

        // The escalation mechanism this ticket's acceptance criteria describe is the same one every other
        // diagnostic in this repository uses (see .editorconfig's `dotnet_diagnostic.<ID>.severity` entries) -
        // exercised here via CSharpCompilationOptions.SpecificDiagnosticOptions, the API-level equivalent of an
        // .editorconfig `dotnet_diagnostic.INTERLOC001.severity = error` entry.
        var escalated = compilation.WithOptions(
            compilation.Options.WithSpecificDiagnosticOptions(
                new System.Collections.Generic.Dictionary<string, ReportDiagnostic> { ["INTERLOC001"] = ReportDiagnostic.Error }
            )
        );

        // Act
        _ = driver.RunGeneratorsAndUpdateCompilation(
            escalated,
            out _,
            out var generatorDiagnostics,
            TestContext.Current.CancellationToken
        );

        // Assert
        var reported = generatorDiagnostics.Single(d => d.Id == "INTERLOC001");
        Assert.Equal(DiagnosticSeverity.Error, reported.Severity);
    }
}
