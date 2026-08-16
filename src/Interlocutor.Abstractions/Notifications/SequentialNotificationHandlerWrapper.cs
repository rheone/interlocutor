namespace Interlocutor.Abstractions.Notifications;

using System.Diagnostics;
using Interlocutor.Abstractions.Diagnostics;

/// <summary>Dispatches a notification to all registered handlers sequentially, stopping on the first failure.</summary>
/// <typeparam name="TNotification">The notification type. Must implement <see cref="INotification"/>.</typeparam>
/// <remarks>
/// <para>
/// Iterates through registered handlers in order of registration, awaiting each one before proceeding to the next.
/// If a handler throws, the exception propagates immediately and subsequent handlers are not invoked. Use this when
/// handler ordering matters or when one handler's failure should prevent dependent handlers from running.
/// </para>
/// </remarks>
internal sealed class SequentialNotificationHandlerWrapper<TNotification> : NotificationHandlerWrapper
    where TNotification : INotification
{
    /// <summary>
    /// Dispatches the notification to all handlers sequentially.
    /// </summary>
    /// <param name="notification">The notification to dispatch.</param>
    /// <param name="services">The service provider for resolving handlers.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <param name="activity">The activity to tag with the resolved handler types, or <see langword="null"/>.</param>
    /// <returns>A task that completes when all handlers complete or the first handler throws.</returns>
    public override async Task Handle(
        object notification,
        IServiceProvider services,
        CancellationToken ct,
        Activity? activity = null
    )
    {
        var typedNotification = (TNotification)notification;
        var handlers = services.GetServices<INotificationHandler<TNotification>>().ToArray();

        if (activity is not null)
        {
            activity.SetTag(
                MediatorActivitySource.HandlerTypeTag,
                string.Join(",", handlers.Select(h => h.GetType().FullName))
            );
        }

        // Execute handlers sequentially in registration order. If a handler throws, the exception propagates
        // immediately and subsequent handlers are not invoked. This is useful when handler ordering matters
        // or when one handler's failure should prevent dependent handlers from running.
        foreach (var handler in handlers)
        {
            await handler.Handle(typedNotification, ct);
        }
    }
}
