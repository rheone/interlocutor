namespace Interlocutor.CommonKernel.Tests;

using Interlocutor.CommonKernel.ResultTypes;

#pragma warning disable SA1402 // One conceptual worked example: the union and its own small case types together.

/// <summary>
/// A seven-case union - one success case plus six independent failure cases - for a payment gateway
/// integration whose distinct decline/error reasons a caller genuinely needs to branch on individually. This is
/// the kind of outlier the shipped 2-6 case <see cref="Outcome{TSuccessResult, TFailureResult}"/> family
/// deliberately doesn't try to cover - see <c>documentation/extensibility.md</c>'s custom-arity worked example,
/// drawn from this file, and <c>documentation/messages-and-result-types.md</c>'s <c>## Choosing Arities</c>
/// guidance on why the shipped family stops at six.
/// </summary>
/// <remarks>
/// Internal rather than public purely because this type exists only to back <see cref="CustomArityOutcomeTests"/>
/// and the documentation excerpt it's drawn from - <c>GenerateOutcomeMembersAttribute</c> itself has no
/// accessibility requirement of its own; a real consumer's own custom-arity union is just as often public.
/// </remarks>
[GenerateOutcomeMembers]
internal readonly partial union PaymentOutcome(
    Receipt,
    InsufficientFunds,
    CardExpired,
    CardDeclined,
    FraudSuspected,
    GatewayTimeout,
    NetworkError
) : IOutcome;

/// <summary>A successful charge's receipt.</summary>
/// <param name="TransactionId">The payment gateway's own transaction identifier.</param>
internal readonly record struct Receipt(string TransactionId)
{
    /// <inheritdoc/>
    public override string ToString() => this.TransactionId;
}

/// <summary>The card had insufficient funds to cover the charge.</summary>
/// <param name="ShortfallAmount">How far short of the charge amount the available balance fell.</param>
internal readonly record struct InsufficientFunds(decimal ShortfallAmount);

/// <summary>The card's expiration date had already passed.</summary>
internal readonly record struct CardExpired;

/// <summary>The issuing bank declined the charge outright.</summary>
/// <param name="ReasonCode">The issuer's own decline reason code (e.g. <c>"do_not_honor"</c>).</param>
internal readonly record struct CardDeclined(string ReasonCode);

/// <summary>The gateway's fraud detection flagged this charge for manual review.</summary>
internal readonly record struct FraudSuspected;

/// <summary>The gateway did not respond within its own service-level time budget.</summary>
internal readonly record struct GatewayTimeout;

/// <summary>An error at the network layer, distinct from a gateway-reported timeout.</summary>
/// <param name="Detail">The underlying network error's own message.</param>
internal readonly record struct NetworkError(string Detail);

#pragma warning restore SA1402
