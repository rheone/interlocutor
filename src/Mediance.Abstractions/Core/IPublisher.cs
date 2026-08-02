namespace Mediance.Abstractions.Core;

using Mediance.Abstractions.Notifications;

/// <summary>Publish-only interface for fire-and-forget notifications. Exposes <see cref="Publish{TNotification}(TNotification,CancellationToken)"/>.</summary>
public interface IPublisher
{
    /// <summary>Publishes a notification to all registered <see cref="INotificationHandler{TNotification}"/> instances.</summary>
    /// <typeparam name="TNotification">The notification type to publish.</typeparam>
    /// <param name="notification">The notification instance to publish.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task Publish<TNotification>(TNotification notification, CancellationToken ct = default)
        where TNotification : INotification;
}
