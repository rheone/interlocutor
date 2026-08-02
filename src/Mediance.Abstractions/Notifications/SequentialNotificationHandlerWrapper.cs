namespace Mediance.Abstractions.Notifications;

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
    public override async Task Handle(object notification, IServiceProvider services, CancellationToken ct)
    {
        var typedNotification = (TNotification)notification;
        var handlers = services.GetServices<INotificationHandler<TNotification>>();

        foreach (var handler in handlers)
        {
            await handler.Handle(typedNotification, ct);
        }
    }
}
