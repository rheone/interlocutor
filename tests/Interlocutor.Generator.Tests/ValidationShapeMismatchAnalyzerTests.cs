namespace Interlocutor.Generator.Tests;

using Microsoft.CodeAnalysis;

/// <summary>
/// Tests for <see cref="ValidationShapeMismatchAnalyzer"/> and <see cref="InterlocutorDiagnostics.ValidationShapeMismatch"/>.
/// </summary>
public sealed class ValidationShapeMismatchAnalyzerTests
{
    private const string CommonUsings = """
        using FluentValidation;
        using Interlocutor.Abstractions.Commands;
        using Interlocutor.Abstractions.Queries;
        using Interlocutor.CommonKernel;
        using Interlocutor.CommonKernel.ResultTypes;

        """;

    [Fact]
    public async Task ValidationShapeMismatch_ResourceCommandHandlerShape_WithValidator_Reports_Test()
    {
        // Arrange - ResourceCommandHandler's fixed Outcome<TSuccess, NotFound, Failure> shape has no case a
        // real validation failure can occupy.
        var source =
            CommonUsings
            + """
                public sealed record FooCommand : ICommand<Outcome<string, NotFound, Failure>>;

                public sealed class FooValidator : AbstractValidator<FooCommand> { }
                """;

        // Act
        var diagnostics = await GeneratorTestHarness.RunAnalyzer(new ValidationShapeMismatchAnalyzer(), source);

        // Assert
        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal("INTERLOC010", diagnostic.Id);
        Assert.Contains("FooCommand", diagnostic.GetMessage(), StringComparison.Ordinal);
        Assert.Contains("FooValidator", diagnostic.GetMessage(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task ValidationShapeMismatch_ValidationCaseAdded_DoesNotReport_Test()
    {
        // Arrange - same command, now with a ValidationErrors case added to the response arity.
        var source =
            CommonUsings
            + """
                public sealed record FooCommand : ICommand<Outcome<string, NotFound, Failure, ValidationErrors>>;

                public sealed class FooValidator : AbstractValidator<FooCommand> { }
                """;

        // Act
        var diagnostics = await GeneratorTestHarness.RunAnalyzer(new ValidationShapeMismatchAnalyzer(), source);

        // Assert
        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task ValidationShapeMismatch_NoValidator_DoesNotReport_Test()
    {
        // Arrange - same shape-mismatched command, but no validator exists for it at all.
        var source =
            CommonUsings
            + """
                public sealed record FooCommand : ICommand<Outcome<string, NotFound, Failure>>;
                """;

        // Act
        var diagnostics = await GeneratorTestHarness.RunAnalyzer(new ValidationShapeMismatchAnalyzer(), source);

        // Assert
        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task ValidationShapeMismatch_QueryWithValidator_Reports_Test()
    {
        // Arrange - the same gotcha applies to a query, not only a command.
        var source =
            CommonUsings
            + """
                public sealed record FooQuery : IQuery<Outcome<string, NotFound>>;

                public sealed class FooQueryValidator : AbstractValidator<FooQuery> { }
                """;

        // Act
        var diagnostics = await GeneratorTestHarness.RunAnalyzer(new ValidationShapeMismatchAnalyzer(), source);

        // Assert
        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal("INTERLOC010", diagnostic.Id);
        Assert.Contains("FooQuery", diagnostic.GetMessage(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task ValidationShapeMismatch_NonGenericOutcomeShape_Reports_Test()
    {
        // Arrange - the shipped non-generic Outcome (Success | Failure) can never carry a validation case.
        var source =
            CommonUsings
            + """
                public sealed record FooCommand : ICommand<Outcome>;

                public sealed class FooValidator : AbstractValidator<FooCommand> { }
                """;

        // Act
        var diagnostics = await GeneratorTestHarness.RunAnalyzer(new ValidationShapeMismatchAnalyzer(), source);

        // Assert
        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal("INTERLOC010", diagnostic.Id);
        Assert.Contains("none", diagnostic.GetMessage(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task ValidationShapeMismatch_CustomNonGenericOutcome_DoesNotReport_Test()
    {
        // Arrange - a fully custom, non-generic IOutcome implementer has no statically-inspectable case set,
        // so the analyzer must not guess and risk a false positive.
        var source =
            CommonUsings
            + """
                public sealed class CustomOutcome : IOutcome;

                public sealed record FooCommand : ICommand<CustomOutcome>;

                public sealed class FooValidator : AbstractValidator<FooCommand> { }
                """;

        // Act
        var diagnostics = await GeneratorTestHarness.RunAnalyzer(new ValidationShapeMismatchAnalyzer(), source);

        // Assert
        Assert.Empty(diagnostics);
    }
}
