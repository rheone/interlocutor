namespace Interlocutor.IntegrationTests;

using Interlocutor.Abstractions.Commands;
using Interlocutor.Abstractions.Markers;
using Interlocutor.CommonKernel;
using Interlocutor.CommonKernel.ResultTypes;

/// <summary>
/// Creates one <see cref="Widget"/> and publishes a <see cref="WidgetCreatedNotification"/> from within its own
/// handler. Carries both <see cref="ITransactionalRequest"/> and <see cref="IRateLimitedRequest"/>, so a single
/// dispatch of this command proves the resilience (<c>RateLimitedBehavior</c>) and transaction
/// (<c>UnitOfWorkBehavior</c>) behaviors interact correctly - a throttled dispatch never reaches the handler,
/// never opens the eager transaction <see cref="ITransactionalRequest"/> asks for, and never publishes the
/// notification, while an allowed dispatch does all three and commits.
/// </summary>
/// <param name="Name">The widget's name.</param>
/// <param name="LimitKey">The rate-limit bucket this dispatch is checked against.</param>
public sealed record CreateWidgetWithNotificationCommand(string Name, string LimitKey)
    : ICommand<Outcome<Success, RateLimited>>,
        ITransactionalRequest,
        IRateLimitedRequest;
