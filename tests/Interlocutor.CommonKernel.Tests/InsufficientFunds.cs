namespace Interlocutor.CommonKernel.Tests;

/// <summary>The card had insufficient funds to cover the charge - one of <see cref="PaymentOutcome"/>'s failure cases.</summary>
/// <param name="ShortfallAmount">How far short of the charge amount the available balance fell.</param>
internal readonly record struct InsufficientFunds(decimal ShortfallAmount);
