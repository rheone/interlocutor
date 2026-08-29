namespace Interlocutor.Abstractions.Notifications;

using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Interlocutor.Abstractions.Core;

/// <summary>
/// Resolves every <see cref="INotificationHandler{TNotification}"/> that applies to a publish of a concrete
/// notification type - true contravariant fan-out, not just handlers registered against the exact closed type.
/// </summary>
/// <remarks>
/// <para>
/// <c>IServiceProvider.GetServices&lt;INotificationHandler&lt;TNotification&gt;&gt;()</c> only finds handlers
/// registered under that exact closed service type - the built-in DI containers do not match variance for
/// closed generic service types. A handler registered as <c>INotificationHandler&lt;DomainEvent&gt;</c> is
/// invisible to a lookup for <c>INotificationHandler&lt;OrderCreated&gt;</c> even though
/// <see cref="INotificationHandler{TNotification}"/> declares <c>TNotification</c> contravariant
/// (<see langword="in"/>). <see cref="ResolveHandlers{TNotification}"/> walks the notification type's
/// base-type chain and implemented interfaces for every level that is itself assignable to
/// <see cref="INotification"/>, resolves handlers registered at each level, and casts each one to
/// <see cref="INotificationHandler{TNotification}"/> - a cast the CLR permits precisely because of that
/// <see langword="in"/> variance.
/// </para>
/// </remarks>
internal static class NotificationHandlerResolver
{
    /// <summary>
    /// Resolves every handler applicable to <typeparamref name="TNotification"/>, ordered per
    /// <see cref="IGeneratedNotificationHandlerOrder"/> when one is registered.
    /// </summary>
    /// <typeparam name="TNotification">The notification's concrete, runtime type.</typeparam>
    /// <param name="services">The service provider to resolve handlers (and the optional order table) from.</param>
    /// <returns>Every matching handler, contravariantly, in the order they should run.</returns>
    public static IReadOnlyList<INotificationHandler<TNotification>> ResolveHandlers<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] TNotification
    >(IServiceProvider services)
        where TNotification : INotification
    {
        var handlers = new List<INotificationHandler<TNotification>>();

        foreach (var level in NotificationTypeHierarchy(typeof(TNotification)))
        {
            var serviceType = typeof(INotificationHandler<>).MakeGenericType(level);
            if (
                services.GetService(typeof(IEnumerable<>).MakeGenericType(serviceType))
                is not System.Collections.IEnumerable resolved
            )
            {
                continue;
            }

            foreach (var handler in resolved)
            {
                // Safe by construction: every `level` here is INotification-assignable, so the handler
                // instance's own INotificationHandler<level> implementation is variance-convertible to
                // INotificationHandler<TNotification> (TNotification : level, and TNotification is `in`).
                handlers.Add((INotificationHandler<TNotification>)handler);
            }
        }

        if (services.GetService(typeof(IGeneratedNotificationHandlerOrder)) is IGeneratedNotificationHandlerOrder order)
        {
            // OrderBy is a stable sort: unranked handlers (Rank == int.MaxValue) keep their relative resolution
            // order among themselves, while ranked handlers move to reflect the compile-time topological order.
            return handlers.OrderBy(h => order.Rank(h.GetType())).ToArray();
        }

        return handlers;
    }

    /// <summary>
    /// Yields <paramref name="notificationType"/> itself, then every base type and implemented interface that
    /// is assignable to <see cref="INotification"/> - the set of service types a handler could legally be
    /// registered against and still apply to a publish of <paramref name="notificationType"/>.
    /// </summary>
    private static IEnumerable<Type> NotificationTypeHierarchy(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] Type notificationType
    )
    {
        var seen = new HashSet<Type>();

        for (
            var current = notificationType;
            current is not null && typeof(INotification).IsAssignableFrom(current);
            current = current.BaseType
        )
        {
            if (seen.Add(current))
            {
                yield return current;
            }
        }

        foreach (
            var candidateInterface in notificationType
                .GetInterfaces()
                .Where(candidateInterface =>
                    typeof(INotification).IsAssignableFrom(candidateInterface) && seen.Add(candidateInterface)
                )
        )
        {
            yield return candidateInterface;
        }
    }
}
