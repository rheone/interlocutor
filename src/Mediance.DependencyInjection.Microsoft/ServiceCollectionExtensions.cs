namespace Microsoft.Extensions.DependencyInjection;

using System.Reflection;
using Mediance.Abstractions;
using Mediance.Abstractions.Commands;
using Mediance.Abstractions.Core;
using Mediance.Abstractions.Notifications;
using Mediance.Abstractions.Queries;

/// <summary>
/// Provides extension methods for registering Mediance services with a Microsoft DI <see cref="IServiceCollection"/>.
/// </summary>
/// <remarks>
/// <para>
/// Use <see cref="AddMediance"/> in your Startup/Program.cs to register all Mediance handlers, behaviors, and the mediator itself.
/// Assembly scanning discovers implementations of <see cref="IQueryHandler{TQuery, TResponse}"/>,
/// <see cref="ICommandHandler{TCommand, TResponse}"/>, <see cref="IVoidCommandHandler{TCommand, TResult}"/>, and
/// <see cref="INotificationHandler{TNotification}"/> without requiring explicit registrations for each handler.
/// </para>
/// </remarks>
public static class MedianceServiceCollectionExtensions
{
    /// <summary>
    /// Scans the specified assemblies for Mediance handler implementations and registers them in the service collection.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to register services into.</param>
    /// <param name="assembliesToScan">The assemblies to scan for handler types.</param>
    /// <returns>The same <see cref="IServiceCollection"/> instance for chaining.</returns>
    /// <remarks>
    /// <para>
    /// The following handler interfaces are discovered and registered as scoped services:
    /// <see cref="IQueryHandler{TQuery,TResponse}"/>, <see cref="ICommandHandler{TCommand,TResponse}"/>,
    /// <see cref="IVoidCommandHandler{TCommand,TResult}"/>, and <see cref="INotificationHandler{TNotification}"/>.
    /// </para>
    /// <para>
    /// Additionally, this method registers the <see cref="Mediator"/> as the implementation for
    /// <see cref="IMediator"/>, <see cref="ISender"/>, and <see cref="IPublisher"/>.
    /// </para>
    /// </remarks>
    public static IServiceCollection AddMediance(this IServiceCollection services, params Assembly[] assembliesToScan)
    {
        // Scan assemblies for concrete, non-abstract types that might implement handler interfaces.
        var handlerTypes = assembliesToScan.SelectMany(a => a.GetTypes()).Where(t => t is { IsClass: true, IsAbstract: false });

        // Register each concrete handler against every matching Mediance handler interface.
        // The mediator resolves handlers by their closed generic contract at runtime, so each
        // registration must map to the exact interface (with specific type parameters) the mediator queries.
        // For example, if MyQueryHandler implements IQueryHandler<GetUserQuery, UserDto>, we register
        // exactly that generic form, not just the open generic.
        foreach (var type in handlerTypes)
        {
            foreach (var iface in type.GetInterfaces())
            {
                if (iface.IsGenericType)
                {
                    var def = iface.GetGenericTypeDefinition();
                    if (
                        def == typeof(IQueryHandler<,>)
                        || def == typeof(ICommandHandler<,>)
                        || def == typeof(IVoidCommandHandler<,>)
                        || def == typeof(INotificationHandler<>)
                    )
                    {
                        services.AddScoped(iface, type);
                    }
                }
            }
        }

        // Register the mediator as the implementation for all three interfaces. ISender and IPublisher
        // are factory-registered to return the same IMediator instance, allowing callers to inject
        // any of the three interfaces and always get the same mediator.
        services.AddScoped<IMediator, Mediator>();
        services.AddScoped<ISender>(sp => sp.GetRequiredService<IMediator>());
        services.AddScoped<IPublisher>(sp => sp.GetRequiredService<IMediator>());

        return services;
    }

    /// <summary>
    /// Scans the assembly containing the specified marker type for Mediance handlers.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to register services into.</param>
    /// <param name="markerType">A type whose containing assembly will be scanned for handler implementations.</param>
    /// <returns>The same <see cref="IServiceCollection"/> instance for chaining.</returns>
    public static IServiceCollection AddMediance(this IServiceCollection services, Type markerType) =>
        services.AddMediance(markerType.Assembly);

    /// <summary>
    /// Scans the entry assembly for Mediance handler implementations.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to register services into.</param>
    /// <returns>The same <see cref="IServiceCollection"/> instance for chaining.</returns>
    public static IServiceCollection AddMedianceFromEntryAssembly(this IServiceCollection services) =>
        services.AddMediance(Assembly.GetEntryAssembly()!);
}
