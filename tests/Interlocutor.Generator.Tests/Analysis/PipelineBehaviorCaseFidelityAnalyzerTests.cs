namespace Interlocutor.Generator.Tests.Analysis;

using Interlocutor.Generator.Analysis;
using Interlocutor.Generator.Diagnostics;
using Microsoft.CodeAnalysis;

/// <summary>
/// Tests for <see cref="PipelineBehaviorCaseFidelityAnalyzer"/> and
/// <see cref="InterlocutorDiagnostics.PipelineBehaviorCaseFidelityMismatch"/>.
/// </summary>
public sealed class PipelineBehaviorCaseFidelityAnalyzerTests
{
    private const string CommonUsings = """
        using Interlocutor.Abstractions.Commands;
        using Interlocutor.Abstractions.Markers;
        using Interlocutor.Abstractions.Queries;
        using Interlocutor.CommonKernel;
        using Interlocutor.CommonKernel.ResultTypes;

        """;

    [Fact]
    public async Task RequiresPolicyRequest_NoForbiddenCase_Reports_Test()
    {
        // Arrange - IRequiresPolicyRequest opts into AuthorizationBehavior, which needs a Forbidden case.
        var source =
            CommonUsings
            + """
                public sealed record FooCommand(string PolicyName) : ICommand<Outcome<string, NotFound>>, IRequiresPolicyRequest;
                """;

        // Act
        var diagnostics = await GeneratorTestHarness.RunAnalyzerAsync(new PipelineBehaviorCaseFidelityAnalyzer(), source);

        // Assert
        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal("INTERLOC015", diagnostic.Id);
        Assert.Contains("FooCommand", diagnostic.GetMessage(), StringComparison.Ordinal);
        Assert.Contains("IRequiresPolicyRequest", diagnostic.GetMessage(), StringComparison.Ordinal);
        Assert.Contains("Forbidden", diagnostic.GetMessage(), StringComparison.Ordinal);
        Assert.Contains("AuthorizationBehavior", diagnostic.GetMessage(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task RequiresPolicyRequest_WithForbiddenCase_DoesNotReport_Test()
    {
        // Arrange - same marker, now with a Forbidden case added to the response arity.
        var source =
            CommonUsings
            + """
                public sealed record FooCommand(string PolicyName) : ICommand<Outcome<string, Forbidden>>, IRequiresPolicyRequest;
                """;

        // Act
        var diagnostics = await GeneratorTestHarness.RunAnalyzerAsync(new PipelineBehaviorCaseFidelityAnalyzer(), source);

        // Assert
        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task CancellableRequest_NoCancelledCase_Reports_Test()
    {
        // Arrange - ICancellableRequest opts into CancellationBehavior, which needs a Cancelled case.
        var source =
            CommonUsings
            + """
                public sealed record FooCommand : ICommand<Outcome<string, NotFound>>, ICancellableRequest;
                """;

        // Act
        var diagnostics = await GeneratorTestHarness.RunAnalyzerAsync(new PipelineBehaviorCaseFidelityAnalyzer(), source);

        // Assert
        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal("INTERLOC015", diagnostic.Id);
        Assert.Contains("ICancellableRequest", diagnostic.GetMessage(), StringComparison.Ordinal);
        Assert.Contains("Cancelled", diagnostic.GetMessage(), StringComparison.Ordinal);
        Assert.Contains("CancellationBehavior", diagnostic.GetMessage(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task CancellableRequest_WithCancelledCase_DoesNotReport_Test()
    {
        // Arrange - same marker, now with a Cancelled case added to the response arity.
        var source =
            CommonUsings
            + """
                public sealed record FooCommand : ICommand<Outcome<string, Cancelled>>, ICancellableRequest;
                """;

        // Act
        var diagnostics = await GeneratorTestHarness.RunAnalyzerAsync(new PipelineBehaviorCaseFidelityAnalyzer(), source);

        // Assert
        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task RateLimitedRequest_NoRateLimitedCase_Reports_Test()
    {
        // Arrange - IRateLimitedRequest opts into RateLimitedBehavior, which needs a RateLimited case.
        var source =
            CommonUsings
            + """
                public sealed record FooQuery(string LimitKey) : IQuery<Outcome<string, NotFound>>, IRateLimitedRequest;
                """;

        // Act
        var diagnostics = await GeneratorTestHarness.RunAnalyzerAsync(new PipelineBehaviorCaseFidelityAnalyzer(), source);

        // Assert
        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal("INTERLOC015", diagnostic.Id);
        Assert.Contains("IRateLimitedRequest", diagnostic.GetMessage(), StringComparison.Ordinal);
        Assert.Contains("RateLimited", diagnostic.GetMessage(), StringComparison.Ordinal);
        Assert.Contains("RateLimitedBehavior", diagnostic.GetMessage(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task RateLimitedRequest_WithRateLimitedCase_DoesNotReport_Test()
    {
        // Arrange - same marker, now with a RateLimited case added to the response arity.
        var source =
            CommonUsings
            + """
                public sealed record FooQuery(string LimitKey) : IQuery<Outcome<string, RateLimited>>, IRateLimitedRequest;
                """;

        // Act
        var diagnostics = await GeneratorTestHarness.RunAnalyzerAsync(new PipelineBehaviorCaseFidelityAnalyzer(), source);

        // Assert
        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task TimeoutRequest_NoTimeoutCase_Reports_Test()
    {
        // Arrange - ITimeoutRequest opts into TimeoutBehavior, which needs a Timeout case.
        var source =
            CommonUsings
            + """
                using System;

                public sealed record FooCommand(TimeSpan Budget) : ICommand<Outcome<string, NotFound>>, ITimeoutRequest;
                """;

        // Act
        var diagnostics = await GeneratorTestHarness.RunAnalyzerAsync(new PipelineBehaviorCaseFidelityAnalyzer(), source);

        // Assert
        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal("INTERLOC015", diagnostic.Id);
        Assert.Contains("ITimeoutRequest", diagnostic.GetMessage(), StringComparison.Ordinal);
        Assert.Contains("Timeout", diagnostic.GetMessage(), StringComparison.Ordinal);
        Assert.Contains("TimeoutBehavior", diagnostic.GetMessage(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task TimeoutRequest_WithTimeoutCase_DoesNotReport_Test()
    {
        // Arrange - same marker, now with a Timeout case added to the response arity.
        var source =
            CommonUsings
            + """
                using System;

                public sealed record FooCommand(TimeSpan Budget) : ICommand<Outcome<string, Timeout>>, ITimeoutRequest;
                """;

        // Act
        var diagnostics = await GeneratorTestHarness.RunAnalyzerAsync(new PipelineBehaviorCaseFidelityAnalyzer(), source);

        // Assert
        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task NoMarkerInterface_DoesNotReport_Test()
    {
        // Arrange - a plain command/query implementing none of the four markers is never checked, regardless
        // of its response arity.
        var source =
            CommonUsings
            + """
                public sealed record FooCommand : ICommand<Outcome<string, NotFound>>;

                public sealed record BarQuery : IQuery<Outcome<string>>;
                """;

        // Act
        var diagnostics = await GeneratorTestHarness.RunAnalyzerAsync(new PipelineBehaviorCaseFidelityAnalyzer(), source);

        // Assert
        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task MultipleMarkersMissingCases_ReportsOnePerMissingCase_Test()
    {
        // Arrange - a request implementing two markers, satisfying neither, gets one diagnostic per marker.
        var source =
            CommonUsings
            + """
                public sealed record FooCommand(string PolicyName)
                    : ICommand<Outcome<string, NotFound>>, IRequiresPolicyRequest, ICancellableRequest;
                """;

        // Act
        var diagnostics = await GeneratorTestHarness.RunAnalyzerAsync(new PipelineBehaviorCaseFidelityAnalyzer(), source);

        // Assert
        Assert.Equal(2, diagnostics.Length);
        Assert.All(diagnostics, d => Assert.Equal("INTERLOC015", d.Id));
    }

    [Fact]
    public async Task NonGenericOutcomeResponse_MarkerImplemented_Reports_Test()
    {
        // Arrange - the shipped, case-less non-generic Outcome can never carry any of the four cases.
        var source =
            CommonUsings
            + """
                public sealed record FooCommand(string PolicyName) : ICommand<Outcome>, IRequiresPolicyRequest;
                """;

        // Act
        var diagnostics = await GeneratorTestHarness.RunAnalyzerAsync(new PipelineBehaviorCaseFidelityAnalyzer(), source);

        // Assert
        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal("INTERLOC015", diagnostic.Id);
    }

    [Fact]
    public async Task CustomNonGenericOutcome_DoesNotReport_Test()
    {
        // Arrange - a fully custom, non-generic IOutcome implementer has no statically-inspectable case set,
        // so the analyzer must not guess and risk a false positive - mirrors
        // ValidationShapeMismatchAnalyzerTests' identical fixture for the same reason.
        var source =
            CommonUsings
            + """
                public sealed class CustomOutcome : IOutcome;

                public sealed record FooCommand(string PolicyName) : ICommand<CustomOutcome>, IRequiresPolicyRequest;
                """;

        // Act
        var diagnostics = await GeneratorTestHarness.RunAnalyzerAsync(new PipelineBehaviorCaseFidelityAnalyzer(), source);

        // Assert
        Assert.Empty(diagnostics);
    }
}
