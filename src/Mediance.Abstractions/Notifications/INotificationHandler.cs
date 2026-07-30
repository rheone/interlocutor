namespace Mediance.Abstractions.Notifications;

/// <summary>Handles a single notification type. Multiple handlers may register for the same <typeparamref name="TNotification"/>.</summary>
/// <typeparam name="TNotification">The notification type this handler consumes.</typeparam>
public interface INotificationHandler<in TNotification>
    where TNotification : INotification
{
    /// <summary>Processes the notification.</summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task Handle(TNotification notification, CancellationToken ct);
}
