namespace TodoTasks.Api.Infrastructure;

/// <summary>
/// The result shape a real third-party transactional-email SDK would hand back from a send call -
/// deliberately not an Interlocutor <c>Outcome</c>/<c>IUnion</c> shape, since a real third-party SDK has
/// no notion of either.
/// <see cref="TodoTasks.Api.Features.Users.SendWelcomeEmail.SendWelcomeEmailHandler"/> adapts this into
/// <see cref="TodoTasks.Api.Features.Users.SendWelcomeEmail.SendWelcomeEmailResult"/>.
/// </summary>
/// <param name="Accepted">
/// Whether the mailer accepted the address for delivery. <see langword="false"/> only when the mailer
/// rejects the address outright - see <see cref="InMemoryWelcomeMailerClient"/>'s own remarks for what
/// this stand-in treats as a rejection - never for a transport failure, which a real SDK would signal by
/// throwing instead of returning a result.
/// </param>
/// <param name="MessageId">
/// The provider-assigned message id, populated only when <paramref name="Accepted"/> is
/// <see langword="true"/>.
/// </param>
/// <param name="RejectionReason">
/// A human-readable rejection reason, populated only when <paramref name="Accepted"/> is
/// <see langword="false"/>.
/// </param>
public readonly record struct WelcomeMailerSendResult(bool Accepted, string? MessageId = null, string? RejectionReason = null);
