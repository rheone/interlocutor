namespace Interlocutor.Generator.Tests;

/// <summary>
/// Covers the three diagnostics <see cref="HandlerDiagnosticAnalyzer"/> reports (extracted from
/// <see cref="InterlocutorSourceGenerator"/>) directly, one case per diagnostic: a missing handler, more than
/// one handler for the same closed query, and a notification handler subscribing to the catch-all base marker.
/// Runs through <see cref="GeneratorTestHarness"/> rather than calling the analyzer's methods in isolation,
/// since <see cref="Microsoft.CodeAnalysis.SourceProductionContext"/> has no accessible constructor outside a
/// real generator run - this still exercises the analyzer on its own, independent of every other diagnostic
/// or emission step the full pipeline performs.
/// </summary>
public sealed class HandlerDiagnosticAnalyzerTests
{
    [Fact]
    public void MissingHandlerDiagnostic_OrphanRequest_Reports_Test()
    {
        // Arrange
        const string source = """
            namespace App;

            using Interlocutor.Abstractions.Queries;
            using Interlocutor.CommonKernel;

            public sealed class OrphanQuery : IQuery<Outcome>;
            """;

        // Act
        var result = GeneratorTestHarness.Run(source);

        // Assert
        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("INTERLOC001", diagnostic.Id);
        Assert.Contains("App.OrphanQuery", diagnostic.GetMessage(), StringComparison.Ordinal);
    }

    [Fact]
    public void DuplicateHandlerDiagnostic_MultipleHandlersSameRequest_ReportsBoth_Test()
    {
        // Arrange
        const string source = """
            namespace App;

            using System.Threading;
            using System.Threading.Tasks;
            using Interlocutor.Abstractions.Queries;
            using Interlocutor.CommonKernel;

            public sealed class DuplicatedQuery : IQuery<Outcome>;

            public sealed class DuplicatedQueryHandlerA : IQueryHandler<DuplicatedQuery, Outcome>
            {
                public Task<Outcome> Handle(DuplicatedQuery query, CancellationToken ct) => throw new System.NotImplementedException();
            }

            public sealed class DuplicatedQueryHandlerB : IQueryHandler<DuplicatedQuery, Outcome>
            {
                public Task<Outcome> Handle(DuplicatedQuery query, CancellationToken ct) => throw new System.NotImplementedException();
            }
            """;

        // Act
        var result = GeneratorTestHarness.Run(source);

        // Assert
        Assert.Equal(2, result.Diagnostics.Length);
        Assert.All(result.Diagnostics, d => Assert.Equal("INTERLOC002", d.Id));
        Assert.Contains(result.Diagnostics, d => d.GetMessage().Contains("DuplicatedQueryHandlerA", StringComparison.Ordinal));
        Assert.Contains(result.Diagnostics, d => d.GetMessage().Contains("DuplicatedQueryHandlerB", StringComparison.Ordinal));
    }

    [Fact]
    public void CatchAllNotificationDiagnostic_BaseMarkerSubscription_Reports_Test()
    {
        // Arrange
        const string source = """
            namespace App;

            using System.Threading;
            using System.Threading.Tasks;
            using Interlocutor.Abstractions.Notifications;

            public sealed class CatchAllHandler : INotificationHandler<INotification>
            {
                public Task Handle(INotification notification, CancellationToken ct) => Task.CompletedTask;
            }
            """;

        // Act
        var result = GeneratorTestHarness.Run(source);

        // Assert
        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("INTERLOC003", diagnostic.Id);
        Assert.Contains("App.CatchAllHandler", diagnostic.GetMessage(), StringComparison.Ordinal);
    }
}
