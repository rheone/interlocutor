namespace Interlocutor.Generator.Tests.Analysis;

using Microsoft.CodeAnalysis;

/// <summary>
/// Tests for the notification handler ordering escape hatch (ADR-0007): the catch-all diagnostic, and the
/// run-after/run-before topological sort - including its cycle diagnostic.
/// </summary>
public sealed class NotificationHandlerOrderingTests
{
    [Fact]
    public void CatchAllDiagnostic_BaseNotificationInterface_Reports_Test()
    {
        // Arrange
        const string source = """
            namespace TestApp;

            using System.Threading;
            using System.Threading.Tasks;
            using Interlocutor.Abstractions.Notifications;

            public sealed class FooHappened : INotification;

            public sealed class AuditEverythingHandler : INotificationHandler<INotification>
            {
                public Task HandleAsync(INotification notification, CancellationToken ct) => Task.CompletedTask;
            }
            """;

        // Act
        var result = GeneratorTestHarness.Run(source);

        // Assert
        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("INTERLOC003", diagnostic.Id);
        Assert.Equal(DiagnosticSeverity.Info, diagnostic.Severity);
        Assert.Contains("AuditEverythingHandler", diagnostic.GetMessage(), StringComparison.Ordinal);
    }

    [Fact]
    public void ConcreteNotificationHandler_Standard_ProducesNoDiagnostic_Test()
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
                public Task HandleAsync(FooHappened notification, CancellationToken ct) => Task.CompletedTask;
            }
            """;

        // Act
        var result = GeneratorTestHarness.Run(source);

        // Assert
        Assert.Empty(result.Diagnostics);
    }

    [Fact]
    public void HandlerOrdering_RunAfter_ProducesLowerRank_Test()
    {
        // Arrange
        const string source = """
            namespace TestApp;

            using System.Threading;
            using System.Threading.Tasks;
            using Interlocutor.Abstractions.Notifications;

            public sealed class FooHappened : INotification;

            public sealed class FirstHandler : INotificationHandler<FooHappened>
            {
                public Task HandleAsync(FooHappened notification, CancellationToken ct) => Task.CompletedTask;
            }

            [NotificationHandlerRunAfter(typeof(FirstHandler))]
            public sealed class SecondHandler : INotificationHandler<FooHappened>
            {
                public Task HandleAsync(FooHappened notification, CancellationToken ct) => Task.CompletedTask;
            }
            """;

        // Act
        var result = GeneratorTestHarness.Run(source);

        // Assert
        Assert.Empty(result.Diagnostics);
        var orderSource = GetGeneratedSource(result, "InterlocutorGeneratedNotificationOrder.g.cs");

        Assert.Contains("[typeof(global::TestApp.FirstHandler)] = 0,", orderSource, StringComparison.Ordinal);
        Assert.Contains("[typeof(global::TestApp.SecondHandler)] = 1,", orderSource, StringComparison.Ordinal);
    }

    [Fact]
    public void HandlerOrdering_RunBefore_EquivalentToRunAfter_Test()
    {
        // Arrange
        const string source = """
            namespace TestApp;

            using System.Threading;
            using System.Threading.Tasks;
            using Interlocutor.Abstractions.Notifications;

            public sealed class FooHappened : INotification;

            [NotificationHandlerRunBefore(typeof(SecondHandler))]
            public sealed class FirstHandler : INotificationHandler<FooHappened>
            {
                public Task HandleAsync(FooHappened notification, CancellationToken ct) => Task.CompletedTask;
            }

            public sealed class SecondHandler : INotificationHandler<FooHappened>
            {
                public Task HandleAsync(FooHappened notification, CancellationToken ct) => Task.CompletedTask;
            }
            """;

        // Act
        var result = GeneratorTestHarness.Run(source);

        // Assert
        Assert.Empty(result.Diagnostics);
        var orderSource = GetGeneratedSource(result, "InterlocutorGeneratedNotificationOrder.g.cs");

        Assert.Contains("[typeof(global::TestApp.FirstHandler)] = 0,", orderSource, StringComparison.Ordinal);
        Assert.Contains("[typeof(global::TestApp.SecondHandler)] = 1,", orderSource, StringComparison.Ordinal);
    }

    [Fact]
    public void HandlerOrdering_UnrelatedHandler_NoRankEntry_Test()
    {
        // Arrange
        const string source = """
            namespace TestApp;

            using System.Threading;
            using System.Threading.Tasks;
            using Interlocutor.Abstractions.Notifications;

            public sealed class FooHappened : INotification;

            public sealed class FirstHandler : INotificationHandler<FooHappened>
            {
                public Task HandleAsync(FooHappened notification, CancellationToken ct) => Task.CompletedTask;
            }

            [NotificationHandlerRunAfter(typeof(FirstHandler))]
            public sealed class SecondHandler : INotificationHandler<FooHappened>
            {
                public Task HandleAsync(FooHappened notification, CancellationToken ct) => Task.CompletedTask;
            }

            public sealed class UnrelatedHandler : INotificationHandler<FooHappened>
            {
                public Task HandleAsync(FooHappened notification, CancellationToken ct) => Task.CompletedTask;
            }
            """;

        // Act
        var result = GeneratorTestHarness.Run(source);

        // Assert
        var orderSource = GetGeneratedSource(result, "InterlocutorGeneratedNotificationOrder.g.cs");
        Assert.DoesNotContain("UnrelatedHandler", orderSource, StringComparison.Ordinal);
    }

    [Fact]
    public void OrderingValidation_Cycle_ReportsErrorNamingBoth_Test()
    {
        // Arrange
        const string source = """
            namespace TestApp;

            using System.Threading;
            using System.Threading.Tasks;
            using Interlocutor.Abstractions.Notifications;

            public sealed class FooHappened : INotification;

            [NotificationHandlerRunAfter(typeof(SecondHandler))]
            public sealed class FirstHandler : INotificationHandler<FooHappened>
            {
                public Task HandleAsync(FooHappened notification, CancellationToken ct) => Task.CompletedTask;
            }

            [NotificationHandlerRunAfter(typeof(FirstHandler))]
            public sealed class SecondHandler : INotificationHandler<FooHappened>
            {
                public Task HandleAsync(FooHappened notification, CancellationToken ct) => Task.CompletedTask;
            }
            """;

        // Act
        var result = GeneratorTestHarness.Run(source);

        // Assert
        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("INTERLOC004", diagnostic.Id);
        Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
        Assert.Contains("FirstHandler", diagnostic.GetMessage(), StringComparison.Ordinal);
        Assert.Contains("SecondHandler", diagnostic.GetMessage(), StringComparison.Ordinal);
    }

    [Fact]
    public void OrderingValidation_RunAfterInvalidType_ReportsError_Test()
    {
        // Arrange
        const string source = """
            namespace TestApp;

            using System.Threading;
            using System.Threading.Tasks;
            using Interlocutor.Abstractions.Notifications;

            public sealed class FooHappened : INotification;

            public sealed class NotAHandler;

            [NotificationHandlerRunAfter(typeof(NotAHandler))]
            public sealed class FirstHandler : INotificationHandler<FooHappened>
            {
                public Task HandleAsync(FooHappened notification, CancellationToken ct) => Task.CompletedTask;
            }
            """;

        // Act
        var result = GeneratorTestHarness.Run(source);

        // Assert
        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("INTERLOC005", diagnostic.Id);
        Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
        Assert.Contains("FirstHandler", diagnostic.GetMessage(), StringComparison.Ordinal);
        Assert.Contains("NotAHandler", diagnostic.GetMessage(), StringComparison.Ordinal);
    }

    [Fact]
    public void OrderingValidation_RunBeforeInvalidType_ReportsError_Test()
    {
        // Arrange
        const string source = """
            namespace TestApp;

            using System.Threading;
            using System.Threading.Tasks;
            using Interlocutor.Abstractions.Notifications;

            public sealed class FooHappened : INotification;

            public sealed class NotAHandler;

            [NotificationHandlerRunBefore(typeof(NotAHandler))]
            public sealed class FirstHandler : INotificationHandler<FooHappened>
            {
                public Task HandleAsync(FooHappened notification, CancellationToken ct) => Task.CompletedTask;
            }
            """;

        // Act
        var result = GeneratorTestHarness.Run(source);

        // Assert
        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("INTERLOC005", diagnostic.Id);
        Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
        Assert.Contains("FirstHandler", diagnostic.GetMessage(), StringComparison.Ordinal);
        Assert.Contains("NotAHandler", diagnostic.GetMessage(), StringComparison.Ordinal);
    }

    [Fact]
    public void OrderingEmission_NoAttributes_EmitsEmptyTableAndCompiles_Test()
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
                public Task HandleAsync(FooHappened notification, CancellationToken ct) => Task.CompletedTask;
            }
            """;

        // Act
        var outputCompilation = GeneratorTestHarness.RunAndUpdate(source, out var generatorDiagnostics);

        // Assert
        Assert.Empty(generatorDiagnostics);
        var errors = outputCompilation
            .GetDiagnostics(TestContext.Current.CancellationToken)
            .Where(d => d.Severity == DiagnosticSeverity.Error)
            .ToList();
        Assert.Empty(errors);
    }

    [Fact]
    public void OrderProvider_Registration_RegisteredAlongsideDispatcher_Test()
    {
        // Arrange
        const string source = """
            namespace TestApp;
            public sealed class Nothing;
            """;

        // Act
        var result = GeneratorTestHarness.Run(source);

        // Assert
        var registrationsSource = GetGeneratedSource(result, "InterlocutorGeneratedRegistrations.g.cs");
        Assert.Contains(
            "new global::Interlocutor.DependencyInjection.Abstractions.InterlocutorRegistration(typeof(global::Interlocutor.Abstractions.Core.IGeneratedNotificationHandlerOrder), typeof(global::Interlocutor.Generated.InterlocutorGeneratedNotificationHandlerOrder), global::Interlocutor.DependencyInjection.Abstractions.RegistrationLifetime.Singleton, global::Interlocutor.DependencyInjection.Abstractions.RegistrationKind.Dispatcher)",
            registrationsSource,
            StringComparison.Ordinal
        );
    }

    private static string GetGeneratedSource(GeneratorDriverRunResult result, string hintNameSuffix) =>
        result
            .Results.Single()
            .GeneratedSources.Single(s => s.HintName.EndsWith(hintNameSuffix, StringComparison.Ordinal))
            .SourceText.ToString();
}
