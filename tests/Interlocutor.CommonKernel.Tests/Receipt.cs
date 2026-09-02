namespace Interlocutor.CommonKernel.Tests;

/// <summary>A successful charge's receipt - <see cref="PaymentOutcome"/>'s success case.</summary>
/// <param name="TransactionId">The payment gateway's own transaction identifier.</param>
internal readonly record struct Receipt(string TransactionId)
{
    /// <inheritdoc/>
    public override string ToString() => this.TransactionId;
}
