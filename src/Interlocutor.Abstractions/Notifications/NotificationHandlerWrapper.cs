namespace Interlocutor.Abstractions.Notifications;

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Interlocutor.Abstractions.Diagnostics;

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
    /// <param name="activity">
    /// The <see cref="Activity"/> the mediator started for this publish, or <see langword="null"/> when nothing is
    /// listening. Concrete wrappers tag it with the resolved handler types once resolution completes; the null case
    /// must stay a no-op so the unlistened dispatch path performs no extra work.
    /// </param>
    /// <returns>A task representing the dispatch operation.</returns>
    public abstract Task Handle(
        object notification,
        IServiceProvider services,
        CancellationToken ct,
        Activity? activity = null
    );

    /// <summary>
    /// Casts <paramref name="notification"/> to <typeparamref name="TNotification"/>, resolves every applicable
    /// handler via <see cref="NotificationHandlerResolver.ResolveHandlers{TNotification}"/>, and tags
    /// <paramref name="activity"/> with the resolved handler types - the three-line preamble every concrete
    /// <see cref="Handle"/> override needs before it starts dispatching, regardless of whether it then dispatches
    /// sequentially or concurrently.
    /// </summary>
    /// <typeparam name="TNotification">The notification's concrete, runtime type.</typeparam>
    /// <param name="notification">The notification to dispatch, as resolved by the mediator's own dispatch code.</param>
    /// <param name="services">The service provider to resolve handlers from.</param>
    /// <param name="activity">The activity to tag with the resolved handler types, or <see langword="null"/>.</param>
    /// <returns>The cast notification and every handler applicable to it, in the order they should run.</returns>
    protected static (TNotification Notification, IReadOnlyList<INotificationHandler<TNotification>> Handlers) ResolveHandlers<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] TNotification
    >(object notification, IServiceProvider services, Activity? activity)
        where TNotification : INotification
    {
        var typedNotification = (TNotification)notification;
        var handlers = NotificationHandlerResolver.ResolveHandlers<TNotification>(services);

        activity?.SetTag(MediatorActivitySource.HandlerTypeTag, string.Join(",", handlers.Select(h => h.GetType().FullName)));

        return (typedNotification, handlers);
    }
}
