namespace TodoTasks.Api.Infrastructure;

/// <summary>
/// The default, in-memory <see cref="IWelcomeMailerClient"/> stand-in - never calls a real mailer.
/// Simulates a real provider bouncing an address by rejecting anything at the <c>.invalid</c>
/// top-level domain, which RFC 2606 reserves for exactly this purpose (an address guaranteed to never
/// resolve), rather than inventing an arbitrary "bad address" convention of this project's own.
/// </summary>
public sealed class InMemoryWelcomeMailerClient : IWelcomeMailerClient
{
    /// <inheritdoc/>
    public Task<WelcomeMailerSendResult> SendAsync(string emailAddress, string displayName, CancellationToken ct) =>
        Task.FromResult(
            emailAddress.EndsWith(".invalid", StringComparison.OrdinalIgnoreCase)
                ? new WelcomeMailerSendResult(
                    Accepted: false,
                    RejectionReason: $"'{emailAddress}' is not a deliverable address."
                )
                : new WelcomeMailerSendResult(Accepted: true, MessageId: Guid.NewGuid().ToString("N"))
        );
}
