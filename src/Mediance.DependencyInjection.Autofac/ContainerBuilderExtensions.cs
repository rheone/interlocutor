namespace Mediance.DependencyInjection.Autofac;

using System.Reflection;
using global::Autofac;
using global::Autofac.Extensions.DependencyInjection;
using Mediance.Abstractions;
using Mediance.Abstractions.Commands;
using Mediance.Abstractions.Core;
using Mediance.Abstractions.Notifications;
using Mediance.Abstractions.Queries;

/// <summary>
/// Provides extension methods for registering Mediance services with an Autofac <see cref="ContainerBuilder"/>.
/// </summary>
/// <remarks>
/// <para>
/// Use the <c>AddMediance</c> method in your Autofac module to register all Mediance handlers, behaviors, and the mediator.
/// Assembly scanning discovers handler implementations without explicit registration for each handler type.
/// </para>
/// <para>
/// Unlike Microsoft DI (where handlers are scoped), Autofac handlers are registered as <c>InstancePerLifetimeScope</c>.
/// </para>
/// </remarks>
public static class MedianceAutofacExtensions
{
    /// <summary>
    /// Scans the specified assemblies for Mediance handler implementations and registers them in the container.
    /// </summary>
    /// <param name="builder">The Autofac <see cref="ContainerBuilder"/> to register services into.</param>
    /// <param name="assembliesToScan">The assemblies to scan for handler types.</param>
    /// <returns>The same <see cref="ContainerBuilder"/> instance for chaining.</returns>
    /// <remarks>
    /// <para>
    /// The following handler interfaces are discovered and registered as instance-per-lifetime-scope services:
    /// <see cref="IQueryHandler{TQuery,TResponse}"/>, <see cref="ICommandHandler{TCommand,TResponse}"/>,
    /// <see cref="IVoidCommandHandler{TCommand,TResult}"/>, and <see cref="INotificationHandler{TNotification}"/>.
    /// </para>
    /// <para>
    /// Additionally, this method registers the <see cref="Mediator"/> as the implementation for
    /// <see cref="IMediator"/>, <see cref="ISender"/>, and <see cref="IPublisher"/>, and configures
    /// an <see cref="AutofacServiceProvider"/> adapter bridging Autofac's lifetime scope to
    /// <see cref="IServiceProvider"/>.
    /// </para>
    /// </remarks>
    public static ContainerBuilder AddMediance(this ContainerBuilder builder, params Assembly[] assembliesToScan)
    {
        // Scan assemblies for concrete, non-abstract types that might implement handler interfaces.
        var handlerTypes = assembliesToScan
            .SelectMany(a => a.GetTypes())
            .Where(t => t is { IsClass: true, IsAbstract: false })
            .ToList();

        // Register each concrete handler against every matching Mediance handler interface.
        // The mediator resolves handlers by their closed generic contract at runtime, so each
        // registration must map to the exact interface (with specific type parameters) the mediator queries.
        // InstancePerLifetimeScope is the Autofac equivalent of Microsoft DI's Scoped.
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
                        builder.RegisterType(type).As(iface).InstancePerLifetimeScope();
                    }
                }
            }
        }

        // Bridge Autofac's lifetime scope to IServiceProvider. The Mediator uses IServiceProvider to resolve
        // handlers, so we need to adapt Autofac's ILifetimeScope to the standard IServiceProvider abstraction.
        // ExternallyOwned() tells Autofac not to manage the provider's lifetime since it's used transitively.
        builder
            .Register(c => new AutofacServiceProvider(c.Resolve<ILifetimeScope>()))
            .As<IServiceProvider>()
            .ExternallyOwned();

        // Register the Mediator as the implementation for all three interfaces (IMediator, ISender, IPublisher).
        // All three references resolve to the same instance, allowing callers to inject any of them and get
        // consistent behavior.
        builder.RegisterType<Mediator>().As<IMediator>().As<ISender>().As<IPublisher>().InstancePerLifetimeScope();

        return builder;
    }

    /// <summary>
    /// Scans the assembly containing the specified marker type for Mediance handlers.
    /// </summary>
    /// <param name="builder">The Autofac <see cref="ContainerBuilder"/> to register services into.</param>
    /// <param name="markerType">A type whose containing assembly will be scanned for handler implementations.</param>
    /// <returns>The same <see cref="ContainerBuilder"/> instance for chaining.</returns>
    public static ContainerBuilder AddMediance(this ContainerBuilder builder, Type markerType) =>
        builder.AddMediance(markerType.Assembly);

    /// <summary>
    /// Scans the entry assembly for Mediance handler implementations.
    /// </summary>
    /// <param name="builder">The Autofac <see cref="ContainerBuilder"/> to register services into.</param>
    /// <returns>The same <see cref="ContainerBuilder"/> instance for chaining.</returns>
    public static ContainerBuilder AddMedianceFromEntryAssembly(this ContainerBuilder builder) =>
        builder.AddMediance(Assembly.GetEntryAssembly()!);
}
