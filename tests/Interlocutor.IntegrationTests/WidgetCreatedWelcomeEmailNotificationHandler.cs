namespace Interlocutor.IntegrationTests;

using Interlocutor.Abstractions.Notifications;

/// <summary>
/// The second real <see cref="INotificationHandler{TNotification}"/> subscriber to
/// <see cref="WidgetCreatedNotification"/> - records a simulated welcome-email send. Runs after
/// <see cref="WidgetCreatedAuditNotificationHandler"/>, per that handler's own
/// <see cref="NotificationHandlerRunBeforeAttribute"/> (ADR-0007).
/// </summary>
/// <param name="log">The shared log both subscribers record into.</param>
public sealed class WidgetCreatedWelcomeEmailNotificationHandler(WidgetNotificationLog log)
    : INotificationHandler<WidgetCreatedNotification>
{
    /// <inheritdoc/>
    public Task HandleAsync(WidgetCreatedNotification notification, CancellationToken ct)
    {
        log.Record(nameof(WidgetCreatedWelcomeEmailNotificationHandler));
        return Task.CompletedTask;
    }
}
