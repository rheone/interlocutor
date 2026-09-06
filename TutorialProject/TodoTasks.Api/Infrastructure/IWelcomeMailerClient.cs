namespace TodoTasks.Api.Infrastructure;

/// <summary>
/// Stand-in for a third-party transactional-email SDK client - a small fake so
/// <see cref="TodoTasks.Api.Features.Users.SendWelcomeEmail.SendWelcomeEmailHandler"/> has a real
/// dependency to adapt, the same way <see cref="ICompletionCounter"/> stands in for a real counter
/// service elsewhere in this project rather than this project building one for real. Exposes only the
/// one operation this project's single caller needs, the way a hand-written wrapper over a slice of a
/// real SDK's surface would.
/// </summary>
public interface IWelcomeMailerClient
{
    /// <summary>Sends a welcome email to a newly onboarded user.</summary>
    /// <param name="emailAddress">The recipient's email address.</param>
    /// <param name="displayName">The recipient's display name, used for the email's greeting.</param>
    /// <param name="ct">A token to cancel the send.</param>
    /// <returns>The mailer's own success/failure result shape - see <see cref="WelcomeMailerSendResult"/>.</returns>
    Task<WelcomeMailerSendResult> SendAsync(string emailAddress, string displayName, CancellationToken ct);
}
