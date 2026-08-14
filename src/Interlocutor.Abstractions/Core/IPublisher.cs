namespace Interlocutor.Abstractions.Core;

using Interlocutor.Abstractions.Notifications;

/// <summary>Publish-only interface for fire-and-forget notifications. Exposes <see cref="Publish{TNotification}(TNotification,CancellationToken)"/>.</summary>
/// <remarks>
/// <para>
/// Use <see cref="IPublisher"/> to dispatch domain events and other side-effect-only notifications. Publishing
/// returns immediately after dispatching to all registered handlers; the caller does not wait for handlers to
/// complete. This pattern is ideal for decoupled workflows like sending emails, logging, or triggering downstream
/// processes.
/// </para>
/// <para>
/// Handler execution strategy defaults to sequential (stops on first exception). Use the overload on
/// <see cref="IMediator"/> to switch to parallel execution.
/// </para>
/// </remarks>
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
