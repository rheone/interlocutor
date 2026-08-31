namespace Interlocutor.CommonKernel.Tests;

/// <summary>
/// Proves <see cref="GenerateOutcomeMembersAttribute"/> works identically for a consumer-declared union outside
/// the shipped 2-6 case arity range - see <see cref="PaymentOutcome"/> and
/// <c>documentation/extensibility.md</c>'s custom-arity worked example, drawn from this file.
/// </summary>
public class CustomArityOutcomeTests
{
    [Fact]
    public void Match_OverEachCase_InvokesMatchingDelegate()
    {
        // Arrange
        PaymentOutcome success = new Receipt("txn_123");
        PaymentOutcome insufficientFunds = new InsufficientFunds(50.00m);
        PaymentOutcome cardExpired = default(CardExpired);
        PaymentOutcome cardDeclined = new CardDeclined("do_not_honor");
        PaymentOutcome fraudSuspected = default(FraudSuspected);
        PaymentOutcome gatewayTimeout = default(GatewayTimeout);
        PaymentOutcome networkError = new NetworkError("connection reset");

        static string Describe(PaymentOutcome outcome) =>
            outcome.Match(
                receipt => $"charged, receipt {receipt.TransactionId}",
                shortfall => $"insufficient funds, short {shortfall.ShortfallAmount}",
                _ => "card expired",
                declined => $"declined: {declined.ReasonCode}",
                _ => "fraud suspected",
                _ => "gateway timeout",
                error => $"network error: {error.Detail}"
            );

        // Act & Assert
        Assert.Equal("charged, receipt txn_123", Describe(success));
        Assert.Equal("insufficient funds, short 50.00", Describe(insufficientFunds));
        Assert.Equal("card expired", Describe(cardExpired));
        Assert.Equal("declined: do_not_honor", Describe(cardDeclined));
        Assert.Equal("fraud suspected", Describe(fraudSuspected));
        Assert.Equal("gateway timeout", Describe(gatewayTimeout));
        Assert.Equal("network error: connection reset", Describe(networkError));
    }

    [Fact]
    public void IsSuccess_OverSuccessAndFailureCases_ReportsCorrectly()
    {
        // Arrange
        PaymentOutcome success = new Receipt("txn_123");
        PaymentOutcome failure = default(CardExpired);

        // Act & Assert
        Assert.True(success.IsSuccess);
        Assert.False(failure.IsSuccess);
    }

    [Fact]
    public void TryGetValue_HitAndMiss()
    {
        // Arrange
        PaymentOutcome success = new Receipt("txn_123");
        PaymentOutcome failure = new NetworkError("connection reset");

        // Act & Assert
        Assert.True(success.TryGetValue(out Receipt receipt));
        Assert.Equal("txn_123", receipt.TransactionId);
        Assert.False(failure.TryGetValue(out Receipt miss));
        Assert.Equal(default, miss);

        Assert.True(failure.TryGetValue(out NetworkError networkError));
        Assert.Equal("connection reset", networkError.Detail);
        Assert.False(success.TryGetValue(out NetworkError missNetworkError));
        Assert.Equal(default, missNetworkError);
    }

    [Fact]
    public void ToString_ForwardsToActiveCase()
    {
        // Arrange
        PaymentOutcome success = new Receipt("txn_123");

        // Act & Assert
        Assert.Equal("txn_123", success.ToString());
    }
}
