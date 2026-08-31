namespace Interlocutor.CommonKernel.Tests;

/// <summary>The issuing bank declined the charge outright - one of <see cref="PaymentOutcome"/>'s failure cases.</summary>
/// <param name="ReasonCode">The issuer's own decline reason code (e.g. <c>"do_not_honor"</c>).</param>
internal readonly record struct CardDeclined(string ReasonCode);
