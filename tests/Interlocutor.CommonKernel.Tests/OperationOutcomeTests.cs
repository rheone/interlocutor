namespace Interlocutor.CommonKernel.Tests;

using Interlocutor.CommonKernel.ResultTypes;

/// <summary>
/// Covers the non-generic, two-case <see cref="OperationOutcome"/> union: construction from each case type,
/// implicit conversion from a bare case value, and exhaustive switch pattern matching.
/// </summary>
public class OperationOutcomeTests
{
    [Fact]
    public void Construct_FromSuccess_IsSuccessTrue()
    {
        var result = new OperationOutcome(default(Success));

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void Construct_FromFailure_IsSuccessFalse()
    {
        var result = new OperationOutcome(new Failure("boom"));

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void ImplicitConversion_FromSuccess_ProducesSuccessOutcome()
    {
        OperationOutcome result = default(Success);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void ImplicitConversion_FromFailure_ProducesFailureOutcome()
    {
        OperationOutcome result = new Failure("boom");

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void Switch_OverSuccess_MatchesSuccessCase()
    {
        OperationOutcome result = default(Success);

        var message = result.Value switch
        {
            Success => "success",
            Failure => "failure",
            _ => throw new InvalidOperationException("Unreachable"),
        };

        Assert.Equal("success", message);
    }

    [Fact]
    public void Switch_OverFailure_MatchesFailureCase()
    {
        OperationOutcome result = new Failure("boom");

        var message = result.Value switch
        {
            Success => "success",
            Failure failure => failure.Reason,
            _ => throw new InvalidOperationException("Unreachable"),
        };

        Assert.Equal("boom", message);
    }
}
