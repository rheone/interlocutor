using Mediance.Abstractions.Notifications;

namespace Mediance.Abstractions.Core;

/// <summary>Publish-only interface for fire-and-forget notifications. Exposes <see cref="Publish{TNotification}(TNotification,CancellationToken)"/>.</summary>
public interface IPublisher
{
    /// <summary>Publishes a notification to all registered <see cref="INotificationHandler{TNotification}"/> instances.</summary>
    Task Publish<TNotification>(TNotification notification, CancellationToken ct = default)
        where TNotification : INotification;
}
