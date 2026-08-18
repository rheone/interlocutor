namespace Interlocutor.CommonKernel.Tests;

using Interlocutor.CommonKernel.ResultTypes;

/// <summary>
/// Tests construction, equality, and default-value behavior of <see cref="Failure"/> and
/// <see cref="Failure{TErrorCode}"/>.
/// </summary>
public class FailureTests
{
    [Fact]
    public void Failure_DefaultValue_ReasonIsNull()
    {
        var defaultValue = default(Failure);
        Assert.Null(defaultValue.Reason);
    }

    [Fact]
    public void Failure_ConstructedWithNamedDefault_ReasonIsNull()
    {
        var failure = new Failure(Reason: null);
        Assert.Null(failure.Reason);
    }

    [Fact]
    public void Failure_WithReason_ReasonIsSet()
    {
        var failure = new Failure("Operation failed");
        Assert.Equal("Operation failed", failure.Reason);
    }

    [Fact]
    public void Failure_SameReason_AreEqual()
    {
        var first = new Failure("Timeout");
        var second = new Failure("Timeout");

        Assert.Equal(first, second);
        Assert.True(first == second);
    }

    [Fact]
    public void Failure_DifferentReason_AreNotEqual()
    {
        var first = new Failure("Timeout");
        var second = new Failure("Disk full");

        Assert.NotEqual(first, second);
        Assert.True(first != second);
    }

    [Fact]
    public void Failure_DefaultValue_EqualsExplicitNullReason()
    {
        var defaultValue = default(Failure);
        var explicitValue = new Failure(null);

        Assert.Equal(defaultValue, explicitValue);
    }

    [Fact]
    public void FailureOfTErrorCode_DefaultValue_ErrorCodeIsDefault()
    {
        var defaultValue = default(Failure<int>);
        Assert.Equal(0, defaultValue.ErrorCode);
        Assert.Null(defaultValue.Reason);
    }

    [Fact]
    public void FailureOfTErrorCode_WithErrorCodeOnly_ReasonIsNull()
    {
        var failure = new Failure<int>(404);
        Assert.Equal(404, failure.ErrorCode);
        Assert.Null(failure.Reason);
    }

    [Fact]
    public void FailureOfTErrorCode_WithErrorCodeAndReason_BothAreSet()
    {
        var failure = new Failure<string>("payment_declined", "The card was declined");

        Assert.Equal("payment_declined", failure.ErrorCode);
        Assert.Equal("The card was declined", failure.Reason);
    }

    [Fact]
    public void FailureOfTErrorCode_SameErrorCodeAndReason_AreEqual()
    {
        var first = new Failure<int>(1001, "Email is required");
        var second = new Failure<int>(1001, "Email is required");

        Assert.Equal(first, second);
        Assert.True(first == second);
    }

    [Fact]
    public void FailureOfTErrorCode_DifferentErrorCode_AreNotEqual()
    {
        var first = new Failure<int>(1001, "Email is required");
        var second = new Failure<int>(1002, "Email is required");

        Assert.NotEqual(first, second);
        Assert.True(first != second);
    }

    [Fact]
    public void FailureOfTErrorCode_DifferentReason_AreNotEqual()
    {
        var first = new Failure<int>(1001, "Email is required");
        var second = new Failure<int>(1001, "Email is missing");

        Assert.NotEqual(first, second);
    }

    [Fact]
    public void Failure_WithNoReason_ReturnsBareTypeName()
    {
        Assert.Equal("Failure", default(Failure).ToString());
    }

    [Fact]
    public void Failure_WithReason_IncludesReason()
    {
        Assert.Equal("Failure: Operation failed", new Failure("Operation failed").ToString());
    }

    [Fact]
    public void FailureOfTErrorCode_WithNoReason_IncludesErrorCodeOnly()
    {
        Assert.Equal("Failure: 404", new Failure<int>(404).ToString());
    }

    [Fact]
    public void FailureOfTErrorCode_WithReason_IncludesErrorCodeAndReason()
    {
        var failure = new Failure<string>("payment_declined", "The card was declined");
        Assert.Equal("Failure: payment_declined - The card was declined", failure.ToString());
    }
}
