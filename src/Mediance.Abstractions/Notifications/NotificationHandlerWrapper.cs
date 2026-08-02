namespace Mediance.Abstractions.Notifications;

/// <summary>Defines the base contract for notification dispatch wrappers.</summary>
/// <remarks>
/// <para>
/// This is an internal abstraction used by the mediator to dispatch notifications. Concrete subclasses
/// (e.g., <c>SequentialNotificationHandlerWrapper</c>, <c>ParallelNotificationHandlerWrapper</c>) implement
/// different dispatch strategies. Wrappers are cached by notification type and strategy to avoid repeated reflection.
/// </para>
/// </remarks>
internal abstract class NotificationHandlerWrapper
{
    /// <summary>
    /// Dispatches the notification to all registered handlers according to the strategy.
    /// </summary>
    /// <param name="notification">The notification to dispatch.</param>
    /// <param name="services">The service provider for resolving handlers.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A task representing the dispatch operation.</returns>
    public abstract Task Handle(object notification, IServiceProvider services, CancellationToken ct);
}
