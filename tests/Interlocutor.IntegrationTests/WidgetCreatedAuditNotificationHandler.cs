namespace Interlocutor.IntegrationTests;

using Interlocutor.Abstractions.Notifications;

/// <summary>
/// One of two real <see cref="INotificationHandler{TNotification}"/> subscribers to
/// <see cref="WidgetCreatedNotification"/> - records an audit entry. Pinned to run before
/// <see cref="WidgetCreatedWelcomeEmailNotificationHandler"/> via <see cref="NotificationHandlerRunBeforeAttribute"/>,
/// so the fan-out order this project asserts on is the documented, compile-time pinned order (ADR-0007), not
/// incidental resolution order.
/// </summary>
/// <param name="log">The shared log both subscribers record into.</param>
[NotificationHandlerRunBefore(typeof(WidgetCreatedWelcomeEmailNotificationHandler))]
public sealed class WidgetCreatedAuditNotificationHandler(WidgetNotificationLog log)
    : INotificationHandler<WidgetCreatedNotification>
{
    /// <inheritdoc/>
    public Task HandleAsync(WidgetCreatedNotification notification, CancellationToken ct)
    {
        log.Record(nameof(WidgetCreatedAuditNotificationHandler));
        return Task.CompletedTask;
    }
}
