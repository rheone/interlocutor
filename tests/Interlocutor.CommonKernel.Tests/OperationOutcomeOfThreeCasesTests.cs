namespace Interlocutor.CommonKernel.Tests;

using Interlocutor.CommonKernel.ResultTypes;

/// <summary>
/// Covers the three-case generic <see cref="OperationOutcome{TSuccessResult, TCase2, TCase3}"/> union: construction
/// from each case type, implicit conversion from a bare case value, and exhaustive switch pattern matching.
/// </summary>
public class OperationOutcomeOfThreeCasesTests
{
    [Fact]
    public void Construct_FromSuccess_IsSuccessTrue()
    {
        var result = new OperationOutcome<string, NotFound, Forbidden>("hello");

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void Construct_FromCase2_IsSuccessFalse()
    {
        var result = new OperationOutcome<string, NotFound, Forbidden>(new NotFound("missing"));

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void Construct_FromCase3_IsSuccessFalse()
    {
        var result = new OperationOutcome<string, NotFound, Forbidden>(new Forbidden("denied"));

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void ImplicitConversion_FromSuccessValue_ProducesSuccessOutcome()
    {
        OperationOutcome<string, NotFound, Forbidden> result = "hello";

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void ImplicitConversion_FromCase2Value_ProducesCase2Outcome()
    {
        OperationOutcome<string, NotFound, Forbidden> result = new NotFound("missing");

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void ImplicitConversion_FromCase3Value_ProducesCase3Outcome()
    {
        OperationOutcome<string, NotFound, Forbidden> result = new Forbidden("denied");

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void Switch_OverSuccess_MatchesSuccessCase()
    {
        OperationOutcome<string, NotFound, Forbidden> result = "hello";

        var message = result.Value switch
        {
            string success => success,
            NotFound => "not found",
            Forbidden => "forbidden",
            _ => throw new InvalidOperationException("Unreachable"),
        };

        Assert.Equal("hello", message);
    }

    [Fact]
    public void Switch_OverCase2_MatchesCase2()
    {
        OperationOutcome<string, NotFound, Forbidden> result = new NotFound("missing");

        var message = result.Value switch
        {
            string success => success,
            NotFound notFound => notFound.Message,
            Forbidden => "forbidden",
            _ => throw new InvalidOperationException("Unreachable"),
        };

        Assert.Equal("missing", message);
    }

    [Fact]
    public void Switch_OverCase3_MatchesCase3()
    {
        OperationOutcome<string, NotFound, Forbidden> result = new Forbidden("denied");

        var message = result.Value switch
        {
            string success => success,
            NotFound => "not found",
            Forbidden forbidden => forbidden.Reason,
            _ => throw new InvalidOperationException("Unreachable"),
        };

        Assert.Equal("denied", message);
    }

    [Fact]
    public void GetValue_OverSuccess_ReturnsUnderlyingValue()
    {
        OperationOutcome<string, NotFound, Forbidden> result = "hello";

        Assert.Equal("hello", result.GetValue());
    }

    [Fact]
    public void GetValue_OverCase2_ReturnsUnderlyingValue()
    {
        var notFound = new NotFound("missing");
        OperationOutcome<string, NotFound, Forbidden> result = notFound;

        Assert.Equal(notFound, result.GetValue());
    }

    [Fact]
    public void GetValue_OverCase3_ReturnsUnderlyingValue()
    {
        var forbidden = new Forbidden("denied");
        OperationOutcome<string, NotFound, Forbidden> result = forbidden;

        Assert.Equal(forbidden, result.GetValue());
    }
}
