namespace Interlocutor.Abstractions.Core;

using Interlocutor.Abstractions.Notifications;

/// <summary>Publish-only interface for one-to-many notification dispatch. Exposes <see cref="PublishAsync{TNotification}(TNotification,CancellationToken)"/>.</summary>
/// <remarks>
/// <para>
/// Use <see cref="IPublisher"/> to dispatch domain events and other side-effect-only notifications to every
/// registered handler for the notification's type. Unlike <see cref="ISender"/>'s request/response dispatch,
/// publishing returns no response value to the caller - the returned <see cref="Task"/> completes once every
/// applicable handler has finished running, not before. This pattern suits decoupled workflows like sending
/// emails, logging, or triggering downstream processes, where the caller needs to know the work happened but
/// has no result to consume from it.
/// </para>
/// <para>
/// Handler execution strategy defaults to <see cref="PublishStrategy.Sequential"/> (stops on first exception).
/// Pass a <see cref="PublishStrategy"/> explicitly to switch to <see cref="PublishStrategy.Parallel"/> execution.
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
    /// <returns>
    /// A <see cref="Task"/> that completes once every applicable handler has finished running, in registration
    /// order, or faults if a handler throws before its turn is reached.
    /// </returns>
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
    /// <returns>
    /// A <see cref="Task"/> that completes once every applicable handler has finished running according to
    /// <paramref name="strategy"/>.
    /// </returns>
    Task PublishAsync<TNotification>(TNotification notification, PublishStrategy strategy, CancellationToken ct = default)
        where TNotification : INotification;
}
