namespace Interlocutor.Abstractions.Notifications;

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

/// <summary>Dispatches a notification to all applicable handlers sequentially, stopping on the first failure.</summary>
/// <typeparam name="TNotification">The notification type. Must implement <see cref="INotification"/>.</typeparam>
/// <remarks>
/// <para>
/// Resolves every handler contravariantly applicable to <typeparamref name="TNotification"/> via
/// <see cref="NotificationHandlerResolver"/> - including handlers registered against a base type or interface -
/// and awaits each one in turn. If a handler throws, the exception propagates immediately and subsequent
/// handlers are not invoked. Execution order is otherwise unspecified except where a
/// <see cref="NotificationHandlerRunAfterAttribute"/>/<see cref="NotificationHandlerRunBeforeAttribute"/> pair
/// pins it (see ADR-0007).
/// </para>
/// </remarks>
internal sealed class SequentialNotificationHandlerWrapper<
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] TNotification
> : NotificationHandlerWrapper
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
        var (typedNotification, handlers) = ResolveHandlers<TNotification>(notification, services, activity);

        // Execute handlers sequentially in the resolved (contravariant, order-table-sorted) order. If a
        // handler throws, the exception propagates immediately and subsequent handlers are not invoked.
        foreach (var handler in handlers)
        {
            await handler.HandleAsync(typedNotification, ct).ConfigureAwait(false);
        }
    }
}
