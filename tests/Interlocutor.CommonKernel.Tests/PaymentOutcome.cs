namespace Interlocutor.CommonKernel.Tests;

using Interlocutor.CommonKernel.ResultTypes;

/// <summary>
/// A seven-case union - one success case plus six independent failure cases - for a payment gateway
/// integration whose distinct decline/error reasons a caller genuinely needs to branch on individually. This is
/// the kind of outlier the shipped 2-6 case <see cref="Outcome{T1, T2}"/> family
/// deliberately doesn't try to cover - see <c>documentation/extensibility.md</c>'s custom-arity worked example,
/// drawn from this file, and <c>documentation/messages-and-result-types.md</c>'s <c>## Choosing Arities</c>
/// guidance on why the shipped family stops at six.
/// </summary>
/// <remarks>
/// Internal rather than public purely because this type exists only to back <see cref="CustomArityOutcomeTests"/>
/// and the documentation excerpt it's drawn from - <c>GenerateOutcomeMembersAttribute</c> itself has no
/// accessibility requirement of its own; a real consumer's own custom-arity union is just as often public.
/// </remarks>
// Seven cases is past the default recommended maximum of six (INTERLOC014) - a deliberate outlier, dismissed
// here the same way documentation/extensibility.md's worked example (drawn from this file) demonstrates.
#pragma warning disable INTERLOC014
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
#pragma warning restore INTERLOC014
