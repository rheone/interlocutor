namespace Interlocutor.Abstractions.Notifications;

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

/// <summary>Dispatches a notification to all registered handlers concurrently using <see cref="Task.WhenAll(System.Collections.Generic.IEnumerable{System.Threading.Tasks.Task})"/>.</summary>
/// <typeparam name="TNotification">The notification type. Must implement <see cref="INotification"/>.</typeparam>
/// <remarks>
/// <para>
/// Launches all handlers concurrently via <see cref="Task.WhenAll(System.Collections.Generic.IEnumerable{System.Threading.Tasks.Task})"/>.
/// If multiple handlers throw, their exceptions are aggregated into an <see cref="AggregateException"/>. Use this
/// when handlers are independent and order doesn't matter - it maximizes throughput at the cost of all-or-nothing failure semantics.
/// </para>
/// </remarks>
internal sealed class ParallelNotificationHandlerWrapper<
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] TNotification
> : NotificationHandlerWrapper
    where TNotification : INotification
{
    /// <summary>
    /// Dispatches the notification to all handlers concurrently.
    /// </summary>
    /// <param name="notification">The notification to dispatch.</param>
    /// <param name="services">The service provider for resolving handlers.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <param name="activity">The activity to tag with the resolved handler types, or <see langword="null"/>.</param>
    /// <returns>A task that completes when all handlers complete or any handler throws.</returns>
    public override Task Handle(object notification, IServiceProvider services, CancellationToken ct, Activity? activity = null)
    {
        var (typedNotification, handlers) = ResolveHandlers<TNotification>(notification, services, activity);

        // Launch all handlers concurrently via Task.WhenAll. This maximizes throughput when handlers are
        // independent. If any handler throws, all tasks are awaited and exceptions are aggregated into
        // an AggregateException (all-or-nothing failure semantics). Use this for performance-critical
        // notifications where order doesn't matter.
        var tasks = handlers.Select(h => h.HandleAsync(typedNotification, ct));
        return Task.WhenAll(tasks);
    }
}
