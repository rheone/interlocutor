namespace Mediance.Abstractions.Notifications;

/// <summary>Dispatches a notification to all registered handlers concurrently using <see cref="Task.WhenAll(System.Collections.Generic.IEnumerable{System.Threading.Tasks.Task})"/>.</summary>
/// <typeparam name="TNotification">The notification type. Must implement <see cref="INotification"/>.</typeparam>
/// <remarks>
/// <para>
/// Launches all handlers concurrently via <see cref="Task.WhenAll(System.Collections.Generic.IEnumerable{System.Threading.Tasks.Task})"/>.
/// If multiple handlers throw, their exceptions are aggregated into an <see cref="AggregateException"/>. Use this
/// when handlers are independent and order doesn't matter — it maximizes throughput at the cost of all-or-nothing failure semantics.
/// </para>
/// </remarks>
internal sealed class ParallelNotificationHandlerWrapper<TNotification> : NotificationHandlerWrapper
    where TNotification : INotification
{
    public override Task Handle(object notification, IServiceProvider services, CancellationToken ct)
    {
        var typedNotification = (TNotification)notification;
        var handlers = services.GetServices<INotificationHandler<TNotification>>();

        // Launch all handlers concurrently via Task.WhenAll. This maximizes throughput when handlers are
        // independent. If any handler throws, all tasks are awaited and exceptions are aggregated into
        // an AggregateException (all-or-nothing failure semantics). Use this for performance-critical
        // notifications where order doesn't matter.
        var tasks = handlers.Select(h => h.Handle(typedNotification, ct));
        return Task.WhenAll(tasks);
    }
}
