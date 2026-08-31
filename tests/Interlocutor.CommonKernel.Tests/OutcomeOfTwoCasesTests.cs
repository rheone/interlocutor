namespace Interlocutor.CommonKernel.Tests;

using Interlocutor.CommonKernel.ResultTypes;

/// <summary>
/// Covers the two-case generic <see cref="Outcome{TSuccessResult, TFailureResult}"/> union: construction
/// from each case type, implicit conversion from a bare case value, and exhaustive switch pattern matching.
/// </summary>
public class OutcomeOfTwoCasesTests
{
    [Fact]
    public void Construct_FromSuccess_IsSuccessTrue()
    {
        // Arrange & Act
        var result = new Outcome<string, NotFound>("hello");

        // Assert
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void Construct_FromFailure_IsSuccessFalse()
    {
        // Arrange & Act
        var result = new Outcome<string, NotFound>(new NotFound("missing"));

        // Assert
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void ImplicitConversion_FromSuccessValue_ProducesSuccessOutcome()
    {
        // Arrange & Act
        Outcome<string, NotFound> result = "hello";

        // Assert
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void ImplicitConversion_FromFailureValue_ProducesFailureOutcome()
    {
        // Arrange & Act
        Outcome<string, NotFound> result = new NotFound("missing");

        // Assert
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void Switch_OverSuccess_MatchesSuccessCase()
    {
        // Arrange
        Outcome<string, NotFound> result = "hello";

        // Act
        var message = result.Value switch
        {
            string success => success,
            NotFound => "not found",
            _ => throw new InvalidOperationException("Unreachable"),
        };

        // Assert
        Assert.Equal("hello", message);
    }

    [Fact]
    public void Switch_OverFailure_MatchesFailureCase()
    {
        // Arrange
        Outcome<string, NotFound> result = new NotFound("missing");

        // Act
        var message = result.Value switch
        {
            string success => success,
            NotFound notFound => notFound.Message,
            _ => throw new InvalidOperationException("Unreachable"),
        };

        // Assert
        Assert.Equal("missing", message);
    }

    [Fact]
    public void Match_OverSuccess_InvokesSuccessDelegate()
    {
        // Arrange
        Outcome<string, NotFound> result = "hello";

        // Act
        var message = result.Match(success => "success:" + success, notFound => "not-found:" + notFound.Message);

        // Assert
        Assert.Equal("success:hello", message);
    }

    [Fact]
    public void Match_OverFailure_InvokesFailureDelegate()
    {
        // Arrange
        Outcome<string, NotFound> result = new NotFound("missing");

        // Act
        var message = result.Match(success => "success:" + success, notFound => "not-found:" + notFound.Message);

        // Assert
        Assert.Equal("not-found:missing", message);
    }

    [Fact]
    public void GeneratedSwitch_OverSuccess_InvokesSuccessAction()
    {
        // Arrange
        Outcome<string, NotFound> result = "hello";
        string? observed = null;

        // Act
        result.Switch(success => observed = "success:" + success, notFound => observed = "not-found:" + notFound.Message);

        // Assert
        Assert.Equal("success:hello", observed);
    }

    [Fact]
    public void GeneratedSwitch_OverFailure_InvokesFailureAction()
    {
        // Arrange
        Outcome<string, NotFound> result = new NotFound("missing");
        string? observed = null;

        // Act
        result.Switch(success => observed = "success:" + success, notFound => observed = "not-found:" + notFound.Message);

        // Assert
        Assert.Equal("not-found:missing", observed);
    }

    [Fact]
    public void TryGetValue_SuccessCaseOnSuccessOutcome_ReturnsTrueAndValue()
    {
        // Arrange
        Outcome<string, NotFound> result = "hello";

        // Act
        var found = result.TryGetValue(out string value);

        // Assert
        Assert.True(found);
        Assert.Equal("hello", value);
    }

    [Fact]
    public void TryGetValue_SuccessCaseOnFailureOutcome_ReturnsFalse()
    {
        // Arrange
        Outcome<string, NotFound> result = new NotFound("missing");

        // Act
        var found = result.TryGetValue(out string value);

        // Assert
        Assert.False(found);
        Assert.Null(value);
    }

    [Fact]
    public void TryGetValue_FailureCaseOnFailureOutcome_ReturnsTrueAndValue()
    {
        // Arrange
        Outcome<string, NotFound> result = new NotFound("missing");

        // Act
        var found = result.TryGetValue(out NotFound value);

        // Assert
        Assert.True(found);
        Assert.Equal("missing", value.Message);
    }

    [Fact]
    public void TryGetValue_FailureCaseOnSuccessOutcome_ReturnsFalse()
    {
        // Arrange
        Outcome<string, NotFound> result = "hello";

        // Act
        var found = result.TryGetValue(out NotFound value);

        // Assert
        Assert.False(found);
        Assert.Equal(default, value);
    }

    [Fact]
    public void ToString_OverSuccess_ForwardsToValue()
    {
        // Arrange
        Outcome<string, NotFound> result = "hello";

        // Act & Assert
        Assert.Equal("hello", result.ToString());
    }

    [Fact]
    public void ToString_OverFailure_ForwardsToValuesOwnToString()
    {
        // Arrange
        Outcome<string, NotFound> result = new NotFound("missing");

        // Act & Assert - NotFound's own ToString() override, not a default struct dump.
        Assert.Equal(new NotFound("missing").ToString(), result.ToString());
    }
}
