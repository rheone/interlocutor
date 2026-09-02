namespace Interlocutor.CommonKernel.Tests;

using Interlocutor.CommonKernel.ResultTypes;

/// <summary>
/// Covers the four-case generic <see cref="Outcome{TSuccessResult, TCase2, TCase3, TCase4}"/> union:
/// construction from each case type, implicit conversion from a bare case value, exhaustive switch pattern
/// matching, and the generated <c>Match</c>/<c>Switch</c>/<c>TryGetValue</c>/<c>ToString</c> member set.
/// </summary>
public class OutcomeOfFourCasesTests
{
    [Fact]
    public void Construct_FromSuccess_IsSuccessTrue()
    {
        // Arrange & Act
        var result = new Outcome<string, NotFound, Forbidden, Conflict>("hello");

        // Assert
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void Construct_FromCase2_IsSuccessFalse()
    {
        // Arrange & Act
        var result = new Outcome<string, NotFound, Forbidden, Conflict>(new NotFound("missing"));

        // Assert
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void Construct_FromCase3_IsSuccessFalse()
    {
        // Arrange & Act
        var result = new Outcome<string, NotFound, Forbidden, Conflict>(new Forbidden("denied"));

        // Assert
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void Construct_FromCase4_IsSuccessFalse()
    {
        // Arrange & Act
        var result = new Outcome<string, NotFound, Forbidden, Conflict>(new Conflict("stale", "CONFLICT"));

        // Assert
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void ImplicitConversion_FromSuccessValue_ProducesSuccessOutcome()
    {
        // Arrange & Act
        Outcome<string, NotFound, Forbidden, Conflict> result = "hello";

        // Assert
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void ImplicitConversion_FromCase2Value_ProducesCase2Outcome()
    {
        // Arrange & Act
        Outcome<string, NotFound, Forbidden, Conflict> result = new NotFound("missing");

        // Assert
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void ImplicitConversion_FromCase3Value_ProducesCase3Outcome()
    {
        // Arrange & Act
        Outcome<string, NotFound, Forbidden, Conflict> result = new Forbidden("denied");

        // Assert
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void ImplicitConversion_FromCase4Value_ProducesCase4Outcome()
    {
        // Arrange & Act
        Outcome<string, NotFound, Forbidden, Conflict> result = new Conflict("stale", "CONFLICT");

        // Assert
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void Switch_OverSuccess_MatchesSuccessCase()
    {
        // Arrange
        Outcome<string, NotFound, Forbidden, Conflict> result = "hello";

        // Act
        var message = result.Value switch
        {
            string success => success,
            NotFound => "not found",
            Forbidden => "forbidden",
            Conflict => "conflict",
            _ => throw new InvalidOperationException("Unreachable"),
        };

        // Assert
        Assert.Equal("hello", message);
    }

    [Fact]
    public void Switch_OverCase2_MatchesCase2()
    {
        // Arrange
        Outcome<string, NotFound, Forbidden, Conflict> result = new NotFound("missing");

        // Act
        var message = result.Value switch
        {
            string success => success,
            NotFound notFound => notFound.Message,
            Forbidden => "forbidden",
            Conflict => "conflict",
            _ => throw new InvalidOperationException("Unreachable"),
        };

        // Assert
        Assert.Equal("missing", message);
    }

    [Fact]
    public void Switch_OverCase3_MatchesCase3()
    {
        // Arrange
        Outcome<string, NotFound, Forbidden, Conflict> result = new Forbidden("denied");

        // Act
        var message = result.Value switch
        {
            string success => success,
            NotFound => "not found",
            Forbidden forbidden => forbidden.Reason,
            Conflict => "conflict",
            _ => throw new InvalidOperationException("Unreachable"),
        };

        // Assert
        Assert.Equal("denied", message);
    }

    [Fact]
    public void Switch_OverCase4_MatchesCase4()
    {
        // Arrange
        Outcome<string, NotFound, Forbidden, Conflict> result = new Conflict("stale", "CONFLICT");

        // Act
        var message = result.Value switch
        {
            string success => success,
            NotFound => "not found",
            Forbidden => "forbidden",
            Conflict conflict => conflict.Message,
            _ => throw new InvalidOperationException("Unreachable"),
        };

        // Assert
        Assert.Equal("stale", message);
    }

    [Fact]
    public void Value_OverSuccess_ReturnsUnderlyingValue()
    {
        // Arrange
        Outcome<string, NotFound, Forbidden, Conflict> result = "hello";

        // Act & Assert
        Assert.Equal("hello", result.Value);
    }

    [Fact]
    public void Value_OverCase4_ReturnsUnderlyingValue()
    {
        // Arrange
        var conflict = new Conflict("stale", "CONFLICT");
        Outcome<string, NotFound, Forbidden, Conflict> result = conflict;

        // Act & Assert
        Assert.Equal(conflict, result.Value);
    }

    [Fact]
    public void Match_OverEachCase_InvokesMatchingDelegate()
    {
        // Arrange
        Outcome<string, NotFound, Forbidden, Conflict> success = "hello";
        Outcome<string, NotFound, Forbidden, Conflict> case2 = new NotFound("missing");
        Outcome<string, NotFound, Forbidden, Conflict> case3 = new Forbidden("denied");
        Outcome<string, NotFound, Forbidden, Conflict> case4 = new Conflict("stale", "CONFLICT");

        static string Project(Outcome<string, NotFound, Forbidden, Conflict> outcome) =>
            outcome.Match(
                s => "success:" + s,
                notFound => "case2:" + notFound.Message,
                forbidden => "case3:" + forbidden.Reason,
                conflict => "case4:" + conflict.Message
            );

        // Act & Assert
        Assert.Equal("success:hello", Project(success));
        Assert.Equal("case2:missing", Project(case2));
        Assert.Equal("case3:denied", Project(case3));
        Assert.Equal("case4:stale", Project(case4));
    }

    [Fact]
    public void Switch_GeneratedOverload_DispatchesToMatchingAction()
    {
        // Arrange
        Outcome<string, NotFound, Forbidden, Conflict> success = "hello";
        Outcome<string, NotFound, Forbidden, Conflict> case2 = new NotFound("missing");
        Outcome<string, NotFound, Forbidden, Conflict> case3 = new Forbidden("denied");
        Outcome<string, NotFound, Forbidden, Conflict> case4 = new Conflict("stale", "CONFLICT");

        static string Observe(Outcome<string, NotFound, Forbidden, Conflict> outcome)
        {
            string result = "unset";
            outcome.Switch(
                s => result = "success:" + s,
                notFound => result = "case2:" + notFound.Message,
                forbidden => result = "case3:" + forbidden.Reason,
                conflict => result = "case4:" + conflict.Message
            );
            return result;
        }

        // Act & Assert
        Assert.Equal("success:hello", Observe(success));
        Assert.Equal("case2:missing", Observe(case2));
        Assert.Equal("case3:denied", Observe(case3));
        Assert.Equal("case4:stale", Observe(case4));
    }

    [Fact]
    public void TryGetValue_Success_HitAndMiss()
    {
        // Arrange
        Outcome<string, NotFound, Forbidden, Conflict> success = "hello";
        Outcome<string, NotFound, Forbidden, Conflict> other = new NotFound("missing");

        // Act & Assert
        Assert.True(success.TryGetValue(out string hit));
        Assert.Equal("hello", hit);
        Assert.False(other.TryGetValue(out string miss));
        Assert.Null(miss);
    }

    [Fact]
    public void TryGetValue_Case2_HitAndMiss()
    {
        // Arrange
        var notFound = new NotFound("missing");
        Outcome<string, NotFound, Forbidden, Conflict> hitOutcome = notFound;
        Outcome<string, NotFound, Forbidden, Conflict> missOutcome = "hello";

        // Act & Assert
        Assert.True(hitOutcome.TryGetValue(out NotFound hit));
        Assert.Equal(notFound, hit);
        Assert.False(missOutcome.TryGetValue(out NotFound miss));
        Assert.Equal(default, miss);
    }

    [Fact]
    public void TryGetValue_Case3_HitAndMiss()
    {
        // Arrange
        var forbidden = new Forbidden("denied");
        Outcome<string, NotFound, Forbidden, Conflict> hitOutcome = forbidden;
        Outcome<string, NotFound, Forbidden, Conflict> missOutcome = "hello";

        // Act & Assert
        Assert.True(hitOutcome.TryGetValue(out Forbidden hit));
        Assert.Equal(forbidden, hit);
        Assert.False(missOutcome.TryGetValue(out Forbidden miss));
        Assert.Equal(default, miss);
    }

    [Fact]
    public void TryGetValue_Case4_HitAndMiss()
    {
        // Arrange
        var conflict = new Conflict("stale", "CONFLICT");
        Outcome<string, NotFound, Forbidden, Conflict> hitOutcome = conflict;
        Outcome<string, NotFound, Forbidden, Conflict> missOutcome = "hello";

        // Act & Assert
        Assert.True(hitOutcome.TryGetValue(out Conflict hit));
        Assert.Equal(conflict, hit);
        Assert.False(missOutcome.TryGetValue(out Conflict miss));
        Assert.Equal(default, miss);
    }

    [Fact]
    public void ToString_ForwardsToActiveCasesOwnToString()
    {
        // Arrange
        Outcome<string, NotFound, Forbidden, Conflict> success = "hello";
        Outcome<string, NotFound, Forbidden, Conflict> case2 = new NotFound("missing");
        Outcome<string, NotFound, Forbidden, Conflict> case3 = new Forbidden("denied");
        Outcome<string, NotFound, Forbidden, Conflict> case4 = new Conflict("stale", "CONFLICT");

        // Act & Assert
        Assert.Equal("hello", success.ToString());
        Assert.Equal(new NotFound("missing").ToString(), case2.ToString());
        Assert.Equal(new Forbidden("denied").ToString(), case3.ToString());
        Assert.Equal(new Conflict("stale", "CONFLICT").ToString(), case4.ToString());
    }
}
