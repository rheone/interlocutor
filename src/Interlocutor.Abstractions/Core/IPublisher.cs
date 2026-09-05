namespace Interlocutor.Abstractions.Core;

using Interlocutor.Abstractions.Notifications;

/// <summary>Publish-only interface for fire-and-forget notifications. Exposes <see cref="PublishAsync{TNotification}(TNotification,CancellationToken)"/>.</summary>
/// <remarks>
/// <para>
/// Use <see cref="IPublisher"/> to dispatch domain events and other side-effect-only notifications. Publishing
/// returns immediately after dispatching to all registered handlers; the caller does not wait for handlers to
/// complete. This pattern is ideal for decoupled workflows like sending emails, logging, or triggering downstream
/// processes.
/// </para>
/// <para>
/// Handler execution strategy defaults to sequential (stops on first exception). Pass a
/// <see cref="PublishStrategy"/> explicitly to switch to parallel execution.
/// </para>
/// </remarks>
public interface IPublisher
{
    /// <summary>
    /// Publishes a notification to every applicable <see cref="INotificationHandler{TNotification}"/>
    /// instance, using <see cref="PublishStrategy.Sequential"/>.
    /// </summary>
    /// <typeparam name="TNotification">The notification type to publish.</typeparam>
    /// <param name="notification">The notification instance to publish.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task PublishAsync<TNotification>(TNotification notification, CancellationToken ct = default)
        where TNotification : INotification;

    /// <summary>
    /// Publishes a notification to every applicable <see cref="INotificationHandler{TNotification}"/>
    /// instance, using <paramref name="strategy"/>.
    /// </summary>
    /// <typeparam name="TNotification">The notification type to publish.</typeparam>
    /// <param name="notification">The notification instance to publish.</param>
    /// <param name="strategy"><see cref="PublishStrategy.Sequential"/> runs handlers in order and stops on failure; <see cref="PublishStrategy.Parallel"/> runs all handlers concurrently.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task PublishAsync<TNotification>(TNotification notification, PublishStrategy strategy, CancellationToken ct = default)
        where TNotification : INotification;
}
