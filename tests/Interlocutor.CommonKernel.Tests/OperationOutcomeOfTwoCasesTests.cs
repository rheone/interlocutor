namespace Interlocutor.CommonKernel.Tests;

using Interlocutor.CommonKernel.ResultTypes;

/// <summary>
/// Covers the two-case generic <see cref="OperationOutcome{TSuccessResult, TFailureResult}"/> union: construction
/// from each case type, implicit conversion from a bare case value, and exhaustive switch pattern matching.
/// </summary>
public class OperationOutcomeOfTwoCasesTests
{
    [Fact]
    public void Construct_FromSuccess_IsSuccessTrue()
    {
        var result = new OperationOutcome<string, NotFound>("hello");

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void Construct_FromFailure_IsSuccessFalse()
    {
        var result = new OperationOutcome<string, NotFound>(new NotFound("missing"));

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void ImplicitConversion_FromSuccessValue_ProducesSuccessOutcome()
    {
        OperationOutcome<string, NotFound> result = "hello";

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void ImplicitConversion_FromFailureValue_ProducesFailureOutcome()
    {
        OperationOutcome<string, NotFound> result = new NotFound("missing");

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void Switch_OverSuccess_MatchesSuccessCase()
    {
        OperationOutcome<string, NotFound> result = "hello";

        var message = result.Value switch
        {
            string success => success,
            NotFound => "not found",
            _ => throw new InvalidOperationException("Unreachable"),
        };

        Assert.Equal("hello", message);
    }

    [Fact]
    public void Switch_OverFailure_MatchesFailureCase()
    {
        OperationOutcome<string, NotFound> result = new NotFound("missing");

        var message = result.Value switch
        {
            string success => success,
            NotFound notFound => notFound.Message,
            _ => throw new InvalidOperationException("Unreachable"),
        };

        Assert.Equal("missing", message);
    }
}
