namespace Interlocutor.Generator.Tests;

using Microsoft.CodeAnalysis;

/// <summary>
/// Tests for the notification handler ordering escape hatch (ADR-0007): the catch-all diagnostic, and the
/// run-after/run-before topological sort - including its cycle diagnostic.
/// </summary>
public sealed class NotificationHandlerOrderingTests
{
    [Fact]
    public void Handler_for_base_notification_interface_reports_catch_all_diagnostic()
    {
        const string source = """
            namespace TestApp;

            using System.Threading;
            using System.Threading.Tasks;
            using Interlocutor.Abstractions.Notifications;

            public sealed class FooHappened : INotification;

            public sealed class AuditEverythingHandler : INotificationHandler<INotification>
            {
                public Task Handle(INotification notification, CancellationToken ct) => Task.CompletedTask;
            }
            """;

        var result = GeneratorTestHarness.Run(source);

        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("INTERLOC003", diagnostic.Id);
        Assert.Equal(DiagnosticSeverity.Info, diagnostic.Severity);
        Assert.Contains("AuditEverythingHandler", diagnostic.GetMessage(), StringComparison.Ordinal);
    }

    [Fact]
    public void Handler_for_a_concrete_notification_reports_no_catch_all_diagnostic()
    {
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

        var result = GeneratorTestHarness.Run(source);

        Assert.Empty(result.Diagnostics);
    }

    [Fact]
    public void RunAfter_produces_a_rank_lower_than_the_handler_it_names()
    {
        const string source = """
            namespace TestApp;

            using System.Threading;
            using System.Threading.Tasks;
            using Interlocutor.Abstractions.Notifications;

            public sealed class FooHappened : INotification;

            public sealed class FirstHandler : INotificationHandler<FooHappened>
            {
                public Task Handle(FooHappened notification, CancellationToken ct) => Task.CompletedTask;
            }

            [NotificationHandlerRunAfter(typeof(FirstHandler))]
            public sealed class SecondHandler : INotificationHandler<FooHappened>
            {
                public Task Handle(FooHappened notification, CancellationToken ct) => Task.CompletedTask;
            }
            """;

        var result = GeneratorTestHarness.Run(source);

        Assert.Empty(result.Diagnostics);
        var orderSource = GetGeneratedSource(result, "InterlocutorGeneratedNotificationOrder.g.cs");

        Assert.Contains("[typeof(global::TestApp.FirstHandler)] = 0,", orderSource, StringComparison.Ordinal);
        Assert.Contains("[typeof(global::TestApp.SecondHandler)] = 1,", orderSource, StringComparison.Ordinal);
    }

    [Fact]
    public void RunBefore_produces_the_same_relative_order_as_the_equivalent_RunAfter()
    {
        const string source = """
            namespace TestApp;

            using System.Threading;
            using System.Threading.Tasks;
            using Interlocutor.Abstractions.Notifications;

            public sealed class FooHappened : INotification;

            [NotificationHandlerRunBefore(typeof(SecondHandler))]
            public sealed class FirstHandler : INotificationHandler<FooHappened>
            {
                public Task Handle(FooHappened notification, CancellationToken ct) => Task.CompletedTask;
            }

            public sealed class SecondHandler : INotificationHandler<FooHappened>
            {
                public Task Handle(FooHappened notification, CancellationToken ct) => Task.CompletedTask;
            }
            """;

        var result = GeneratorTestHarness.Run(source);

        Assert.Empty(result.Diagnostics);
        var orderSource = GetGeneratedSource(result, "InterlocutorGeneratedNotificationOrder.g.cs");

        Assert.Contains("[typeof(global::TestApp.FirstHandler)] = 0,", orderSource, StringComparison.Ordinal);
        Assert.Contains("[typeof(global::TestApp.SecondHandler)] = 1,", orderSource, StringComparison.Ordinal);
    }

    [Fact]
    public void Handler_uninvolved_in_any_ordering_relationship_gets_no_rank_entry()
    {
        const string source = """
            namespace TestApp;

            using System.Threading;
            using System.Threading.Tasks;
            using Interlocutor.Abstractions.Notifications;

            public sealed class FooHappened : INotification;

            public sealed class FirstHandler : INotificationHandler<FooHappened>
            {
                public Task Handle(FooHappened notification, CancellationToken ct) => Task.CompletedTask;
            }

            [NotificationHandlerRunAfter(typeof(FirstHandler))]
            public sealed class SecondHandler : INotificationHandler<FooHappened>
            {
                public Task Handle(FooHappened notification, CancellationToken ct) => Task.CompletedTask;
            }

            public sealed class UnrelatedHandler : INotificationHandler<FooHappened>
            {
                public Task Handle(FooHappened notification, CancellationToken ct) => Task.CompletedTask;
            }
            """;

        var result = GeneratorTestHarness.Run(source);

        var orderSource = GetGeneratedSource(result, "InterlocutorGeneratedNotificationOrder.g.cs");
        Assert.DoesNotContain("UnrelatedHandler", orderSource, StringComparison.Ordinal);
    }

    [Fact]
    public void Ordering_cycle_reports_an_error_diagnostic_naming_the_cycle()
    {
        const string source = """
            namespace TestApp;

            using System.Threading;
            using System.Threading.Tasks;
            using Interlocutor.Abstractions.Notifications;

            public sealed class FooHappened : INotification;

            [NotificationHandlerRunAfter(typeof(SecondHandler))]
            public sealed class FirstHandler : INotificationHandler<FooHappened>
            {
                public Task Handle(FooHappened notification, CancellationToken ct) => Task.CompletedTask;
            }

            [NotificationHandlerRunAfter(typeof(FirstHandler))]
            public sealed class SecondHandler : INotificationHandler<FooHappened>
            {
                public Task Handle(FooHappened notification, CancellationToken ct) => Task.CompletedTask;
            }
            """;

        var result = GeneratorTestHarness.Run(source);

        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("INTERLOC004", diagnostic.Id);
        Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
        Assert.Contains("FirstHandler", diagnostic.GetMessage(), StringComparison.Ordinal);
        Assert.Contains("SecondHandler", diagnostic.GetMessage(), StringComparison.Ordinal);
    }

    [Fact]
    public void RunAfter_naming_a_type_that_is_not_a_notification_handler_reports_an_error_diagnostic()
    {
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
                public Task Handle(FooHappened notification, CancellationToken ct) => Task.CompletedTask;
            }
            """;

        var result = GeneratorTestHarness.Run(source);

        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("INTERLOC005", diagnostic.Id);
        Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
        Assert.Contains("FirstHandler", diagnostic.GetMessage(), StringComparison.Ordinal);
        Assert.Contains("NotAHandler", diagnostic.GetMessage(), StringComparison.Ordinal);
    }

    [Fact]
    public void RunBefore_naming_a_type_that_is_not_a_notification_handler_reports_an_error_diagnostic()
    {
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
                public Task Handle(FooHappened notification, CancellationToken ct) => Task.CompletedTask;
            }
            """;

        var result = GeneratorTestHarness.Run(source);

        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("INTERLOC005", diagnostic.Id);
        Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
        Assert.Contains("FirstHandler", diagnostic.GetMessage(), StringComparison.Ordinal);
        Assert.Contains("NotAHandler", diagnostic.GetMessage(), StringComparison.Ordinal);
    }

    [Fact]
    public void No_ordering_attributes_emits_an_empty_order_table_and_still_compiles()
    {
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

        var outputCompilation = GeneratorTestHarness.RunAndUpdate(source, out var generatorDiagnostics);

        Assert.Empty(generatorDiagnostics);
        var errors = outputCompilation
            .GetDiagnostics(TestContext.Current.CancellationToken)
            .Where(d => d.Severity == DiagnosticSeverity.Error)
            .ToList();
        Assert.True(errors.Count == 0, string.Join(Environment.NewLine, errors.Select(e => e.ToString())));
    }

    [Fact]
    public void Notification_handler_order_provider_is_registered_alongside_the_dispatcher()
    {
        const string source = """
            namespace TestApp;
            public sealed class Nothing;
            """;

        var result = GeneratorTestHarness.Run(source);

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
