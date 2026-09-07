namespace TodoTasks.Api.Features.Users.SendWelcomeEmail;

using Interlocutor.Abstractions.Commands;
using Interlocutor.CommonKernel.ResultTypes;
using Interlocutor.Persistence.Abstractions;
using TodoTasks.Api.Domain;
using TodoTasks.Api.Infrastructure;

/// <summary>Handles <see cref="SendWelcomeEmailCommand"/>.</summary>
public sealed class SendWelcomeEmailHandler(IDomainRepository<User, int> users, IWelcomeMailerClient mailer)
    : ICommandHandler<SendWelcomeEmailCommand, SendWelcomeEmailResult>
{
    /// <inheritdoc/>
    public async Task<SendWelcomeEmailResult> HandleAsync(SendWelcomeEmailCommand command, CancellationToken ct)
    {
        if (users.GetById(command.UserId) is not { } user)
        {
            return SendWelcomeEmailResult.FromFailure(new NotFound($"User {command.UserId}"));
        }

        // IWelcomeMailerClient is a stand-in for a real third-party SDK client - its own success/failure
        // vocabulary (WelcomeMailerSendResult), unrelated to Outcome/IUnion. Adapting that vocabulary into
        // SendWelcomeEmailResult below is the actual point of this feature. The provider-assigned
        // MessageId isn't carried into the response - see SendWelcomeEmailResult's own remarks for why
        // its success case is the bare Success marker rather than a payload that includes it.
        var sendResult = await mailer.SendAsync(user.Email, user.DisplayName, ct).ConfigureAwait(false);

        return sendResult.Accepted
            ? SendWelcomeEmailResult.FromSuccess(default)
            : SendWelcomeEmailResult.FromFailure(new Failure(sendResult.RejectionReason));
    }
}
