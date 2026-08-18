namespace Interlocutor.CommonKernel.Tests;

using Interlocutor.CommonKernel.ResultTypes;

/// <summary>
/// Covers the four-case generic <see cref="OperationOutcome{TSuccessResult, TCase2, TCase3, TCase4}"/> union:
/// construction from each case type, implicit conversion from a bare case value, and exhaustive switch pattern
/// matching.
/// </summary>
public class OperationOutcomeOfFourCasesTests
{
    [Fact]
    public void Construct_FromSuccess_IsSuccessTrue()
    {
        var result = new OperationOutcome<string, NotFound, Forbidden, Conflict>("hello");

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void Construct_FromCase2_IsSuccessFalse()
    {
        var result = new OperationOutcome<string, NotFound, Forbidden, Conflict>(new NotFound("missing"));

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void Construct_FromCase3_IsSuccessFalse()
    {
        var result = new OperationOutcome<string, NotFound, Forbidden, Conflict>(new Forbidden("denied"));

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void Construct_FromCase4_IsSuccessFalse()
    {
        var result = new OperationOutcome<string, NotFound, Forbidden, Conflict>(new Conflict("stale", "CONFLICT"));

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void ImplicitConversion_FromSuccessValue_ProducesSuccessOutcome()
    {
        OperationOutcome<string, NotFound, Forbidden, Conflict> result = "hello";

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void ImplicitConversion_FromCase2Value_ProducesCase2Outcome()
    {
        OperationOutcome<string, NotFound, Forbidden, Conflict> result = new NotFound("missing");

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void ImplicitConversion_FromCase3Value_ProducesCase3Outcome()
    {
        OperationOutcome<string, NotFound, Forbidden, Conflict> result = new Forbidden("denied");

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void ImplicitConversion_FromCase4Value_ProducesCase4Outcome()
    {
        OperationOutcome<string, NotFound, Forbidden, Conflict> result = new Conflict("stale", "CONFLICT");

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void Switch_OverSuccess_MatchesSuccessCase()
    {
        OperationOutcome<string, NotFound, Forbidden, Conflict> result = "hello";

        var message = result.Value switch
        {
            string success => success,
            NotFound => "not found",
            Forbidden => "forbidden",
            Conflict => "conflict",
            _ => throw new InvalidOperationException("Unreachable"),
        };

        Assert.Equal("hello", message);
    }

    [Fact]
    public void Switch_OverCase2_MatchesCase2()
    {
        OperationOutcome<string, NotFound, Forbidden, Conflict> result = new NotFound("missing");

        var message = result.Value switch
        {
            string success => success,
            NotFound notFound => notFound.Message,
            Forbidden => "forbidden",
            Conflict => "conflict",
            _ => throw new InvalidOperationException("Unreachable"),
        };

        Assert.Equal("missing", message);
    }

    [Fact]
    public void Switch_OverCase3_MatchesCase3()
    {
        OperationOutcome<string, NotFound, Forbidden, Conflict> result = new Forbidden("denied");

        var message = result.Value switch
        {
            string success => success,
            NotFound => "not found",
            Forbidden forbidden => forbidden.Reason,
            Conflict => "conflict",
            _ => throw new InvalidOperationException("Unreachable"),
        };

        Assert.Equal("denied", message);
    }

    [Fact]
    public void Switch_OverCase4_MatchesCase4()
    {
        OperationOutcome<string, NotFound, Forbidden, Conflict> result = new Conflict("stale", "CONFLICT");

        var message = result.Value switch
        {
            string success => success,
            NotFound => "not found",
            Forbidden => "forbidden",
            Conflict conflict => conflict.Message,
            _ => throw new InvalidOperationException("Unreachable"),
        };

        Assert.Equal("stale", message);
    }

    [Fact]
    public void GetValue_OverSuccess_ReturnsUnderlyingValue()
    {
        OperationOutcome<string, NotFound, Forbidden, Conflict> result = "hello";

        Assert.Equal("hello", result.GetValue());
    }

    [Fact]
    public void GetValue_OverCase4_ReturnsUnderlyingValue()
    {
        var conflict = new Conflict("stale", "CONFLICT");
        OperationOutcome<string, NotFound, Forbidden, Conflict> result = conflict;

        Assert.Equal(conflict, result.GetValue());
    }
}
