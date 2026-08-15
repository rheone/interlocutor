namespace Interlocutor.DependencyInjection.Abstractions;

using System.Reflection;
using Interlocutor.Abstractions.Commands;
using Interlocutor.Abstractions.Notifications;
using Interlocutor.Abstractions.Queries;

/// <summary>
/// Scans assemblies for concrete implementations of the mediator's handler contracts and expresses
/// each match as a container-neutral <see cref="InterlocutorRegistration"/>.
/// </summary>
/// <remarks>
/// This is the single copy of the handler-scanning logic previously duplicated, byte-for-byte,
/// between <c>Interlocutor.DependencyInjection.Microsoft</c> and <c>Interlocutor.DependencyInjection.Autofac</c>.
/// Both packages call <see cref="Discover(IEnumerable{Assembly})"/> and translate the result into
/// their own container's registration primitives instead of scanning independently.
/// </remarks>
public static class HandlerDiscovery
{
    /// <summary>
    /// The open generic handler contracts a concrete type is checked against.
    /// </summary>
    private static readonly Type[] HandlerContracts =
    [
        typeof(IQueryHandler<,>),
        typeof(ICommandHandler<,>),
        typeof(IVoidCommandHandler<,>),
        typeof(INotificationHandler<>),
    ];

    /// <summary>
    /// Scans the given assemblies for concrete, non-abstract classes implementing any of the
    /// mediator's handler contracts (<see cref="IQueryHandler{TQuery,TResponse}"/>,
    /// <see cref="ICommandHandler{TCommand,TResponse}"/>, <see cref="IVoidCommandHandler{TCommand,TResult}"/>,
    /// or <see cref="INotificationHandler{TNotification}"/>).
    /// </summary>
    /// <param name="assembliesToScan">The assemblies to scan for handler types.</param>
    /// <returns>
    /// One <see cref="InterlocutorRegistration"/> per matching handler interface a type implements,
    /// each with <see cref="RegistrationKind.Handler"/> and <see cref="RegistrationLifetime.Scoped"/>.
    /// A type implementing more than one handler contract yields one registration per contract.
    /// </returns>
    public static IReadOnlyList<InterlocutorRegistration> Discover(IEnumerable<Assembly> assembliesToScan)
    {
        var handlerTypes = assembliesToScan.SelectMany(a => a.GetTypes()).Where(t => t is { IsClass: true, IsAbstract: false });

        var registrations = new List<InterlocutorRegistration>();

        // Register each concrete handler against every matching Interlocutor handler interface.
        // The mediator resolves handlers by their closed generic contract at runtime, so each
        // registration must map to the exact interface (with specific type parameters) the mediator queries.
        foreach (var type in handlerTypes)
        {
            foreach (var iface in type.GetInterfaces())
            {
                if (iface.IsGenericType && HandlerContracts.Contains(iface.GetGenericTypeDefinition()))
                {
                    registrations.Add(
                        new InterlocutorRegistration(iface, type, RegistrationLifetime.Scoped, RegistrationKind.Handler)
                    );
                }
            }
        }

        return registrations;
    }
}
