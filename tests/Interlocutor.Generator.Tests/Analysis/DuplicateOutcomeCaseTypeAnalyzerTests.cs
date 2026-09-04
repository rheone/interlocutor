namespace Interlocutor.Generator.Tests.Analysis;

using Interlocutor.Generator.Analysis;
using Interlocutor.Generator.Diagnostics;
using Microsoft.CodeAnalysis;

/// <summary>
/// Tests for <see cref="DuplicateOutcomeCaseTypeAnalyzer"/> and <see cref="InterlocutorDiagnostics.DuplicateOutcomeCaseType"/>.
/// </summary>
public sealed class DuplicateOutcomeCaseTypeAnalyzerTests
{
    private const string CommonUsings = """
        using Interlocutor.CommonKernel;
        using Interlocutor.CommonKernel.ResultTypes;

        """;

    [Fact]
    public async Task DuplicateOutcomeCaseType_TwoCaseUnion_Reports_Test()
    {
        // Arrange
        var source =
            CommonUsings
            + """
                public sealed class Sample
                {
                    public Outcome<Failure, Failure> Get() => default;
                }
                """;

        // Act
        var diagnostics = await GeneratorTestHarness.RunAnalyzer(new DuplicateOutcomeCaseTypeAnalyzer(), source);

        // Assert
        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal("INTERLOC009", diagnostic.Id);
        Assert.Contains("Failure", diagnostic.GetMessage(), StringComparison.Ordinal);
        Assert.Contains("1, 2", diagnostic.GetMessage(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task DuplicateOutcomeCaseType_SuccessPositionRepeated_Reports_Test()
    {
        // Arrange - the success-position type (position 1) repeated as a later failure case.
        var source =
            CommonUsings
            + """
                public sealed class Sample
                {
                    public Outcome<Success, NotFound, Success> Get() => default;
                }
                """;

        // Act
        var diagnostics = await GeneratorTestHarness.RunAnalyzer(new DuplicateOutcomeCaseTypeAnalyzer(), source);

        // Assert
        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal("INTERLOC009", diagnostic.Id);
        Assert.Contains("Success", diagnostic.GetMessage(), StringComparison.Ordinal);
        Assert.Contains("1, 3", diagnostic.GetMessage(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task DuplicateOutcomeCaseType_SixCaseUnion_Reports_Test()
    {
        // Arrange
        var source =
            CommonUsings
            + """
                public sealed class Sample
                {
                    public Outcome<Success, NotFound, Forbidden, Conflict, Failure, Forbidden> Get() => default;
                }
                """;

        // Act
        var diagnostics = await GeneratorTestHarness.RunAnalyzer(new DuplicateOutcomeCaseTypeAnalyzer(), source);

        // Assert
        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal("INTERLOC009", diagnostic.Id);
        Assert.Contains("Forbidden", diagnostic.GetMessage(), StringComparison.Ordinal);
        Assert.Contains("3, 6", diagnostic.GetMessage(), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("public Outcome<Success, Failure> Get() => default;")]
    [InlineData("public Outcome<Success, NotFound, Forbidden> Get() => default;")]
    [InlineData("public Outcome<Success, NotFound, Forbidden, Conflict> Get() => default;")]
    [InlineData("public Outcome<Success, NotFound, Forbidden, Conflict, Failure> Get() => default;")]
    [InlineData("public Outcome<Success, NotFound, Forbidden, Conflict, Failure, ValidationError> Get() => default;")]
    public async Task DuplicateOutcomeCaseType_CleanUnion_DoesNotReport_Test(string member)
    {
        // Arrange
        var source = CommonUsings + "public sealed class Sample { " + member + " }";

        // Act
        var diagnostics = await GeneratorTestHarness.RunAnalyzer(new DuplicateOutcomeCaseTypeAnalyzer(), source);

        // Assert
        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task DuplicateOutcomeCaseType_ConsumerDeclaredUnion_Reports_Test()
    {
        // Arrange - the analyzer must not be hardcoded to this library's own Outcome<...> family.
        var source =
            CommonUsings
            + """
                public readonly struct CustomOutcome<TSuccess, TCase2, TCase3> : IOutcome
                {
                }

                public sealed class Sample
                {
                    public CustomOutcome<Success, Failure, Failure> Get() => default;
                }
                """;

        // Act
        var diagnostics = await GeneratorTestHarness.RunAnalyzer(new DuplicateOutcomeCaseTypeAnalyzer(), source);

        // Assert
        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal("INTERLOC009", diagnostic.Id);
        Assert.Contains("CustomOutcome", diagnostic.GetMessage(), StringComparison.Ordinal);
    }
}
