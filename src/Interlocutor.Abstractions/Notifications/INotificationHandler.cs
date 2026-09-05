namespace Interlocutor.Abstractions.Notifications;

/// <summary>Handles a single notification type. Multiple handlers may register for the same <typeparamref name="TNotification"/>.</summary>
/// <typeparam name="TNotification">The notification type this handler consumes.</typeparam>
/// <remarks>
/// <para>
/// Implement this interface to react to domain events and other notifications published via <see cref="Core.IPublisher"/>.
/// Unlike queries and commands, which have exactly one handler each, multiple handlers can subscribe to the same
/// notification type. This enables loose coupling - the publisher doesn't know or care how many subscribers exist.
/// </para>
/// <para>
/// Handlers run according to the publish strategy (default sequential, can switch to parallel). In sequential mode,
/// exceptions stop further handlers. In parallel mode, all handlers run concurrently and exceptions are aggregated.
/// </para>
/// <para>
/// Notification handlers do not run through the full pipeline behavior chain - they're invoked directly by the
/// publisher after being resolved from DI. This keeps them lightweight and decoupled.
/// </para>
/// </remarks>
public interface INotificationHandler<in TNotification>
    where TNotification : INotification
{
    /// <summary>
    /// Processes the notification. Superseded by <see cref="HandleAsync"/> - implement that member in new
    /// handlers instead.
    /// </summary>
    /// <param name="notification">The notification to handle.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    /// <remarks>
    /// Carries no <c>Async</c> suffix despite returning a <see cref="Task"/>, unlike the rest of this
    /// codebase's awaitable-returning members - <see cref="HandleAsync"/> carries the suffix instead. Existing
    /// implementations of this member keep working - the default <see cref="HandleAsync"/> forwards to it - but
    /// new handlers should implement <see cref="HandleAsync"/> instead.
    /// </remarks>
    Task Handle(TNotification notification, CancellationToken ct);

    /// <summary>The preferred, <c>Async</c>-suffixed member going forward: processes the notification.</summary>
    /// <param name="notification">The notification to handle.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    /// <remarks>
    /// The default implementation forwards to <see cref="Handle"/> so every existing implementer keeps
    /// compiling and behaving identically until it migrates to overriding this member instead.
    /// </remarks>
    Task HandleAsync(TNotification notification, CancellationToken ct) => this.Handle(notification, ct);
}
