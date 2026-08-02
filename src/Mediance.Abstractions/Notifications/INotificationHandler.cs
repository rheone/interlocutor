namespace Mediance.Abstractions.Notifications;

/// <summary>Handles a single notification type. Multiple handlers may register for the same <typeparamref name="TNotification"/>.</summary>
/// <typeparam name="TNotification">The notification type this handler consumes.</typeparam>
/// <remarks>
/// <para>
/// Implement this interface to react to domain events and other notifications published via <see cref="IPublisher"/>.
/// Unlike queries and commands, which have exactly one handler each, multiple handlers can subscribe to the same
/// notification type. This enables loose coupling — the publisher doesn't know or care how many subscribers exist.
/// </para>
/// <para>
/// Handlers run according to the publish strategy (default sequential, can switch to parallel). In sequential mode,
/// exceptions stop further handlers. In parallel mode, all handlers run concurrently and exceptions are aggregated.
/// Use <see cref="PublishStrategy"/> to control this behavior.
/// </para>
/// <para>
/// Notification handlers do not run through the full pipeline behavior chain — they're invoked directly by the
/// publisher after being resolved from DI. This keeps them lightweight and decoupled.
/// </para>
/// </remarks>
public interface INotificationHandler<in TNotification>
    where TNotification : INotification
{
    /// <summary>Processes the notification.</summary>
    /// <param name="notification">The notification to handle.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task Handle(TNotification notification, CancellationToken ct);
}
