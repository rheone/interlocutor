namespace Interlocutor.DependencyInjection.Microsoft;

using System.Reflection;
using global::Microsoft.Extensions.DependencyInjection;
using global::Microsoft.Extensions.DependencyInjection.Extensions;
using Interlocutor.Abstractions;
using Interlocutor.Abstractions.Commands;
using Interlocutor.Abstractions.Core;
using Interlocutor.Abstractions.Notifications;
using Interlocutor.Abstractions.Pipeline;
using Interlocutor.Abstractions.Pipeline.Classifiers;
using Interlocutor.Abstractions.Queries;
using Interlocutor.DependencyInjection.Abstractions;

/// <summary>
/// Provides extension methods for registering Interlocutor services with a Microsoft DI <see cref="IServiceCollection"/>.
/// </summary>
/// <remarks>
/// <para>
/// Use <see cref="AddInterlocutor(IServiceCollection, Assembly[])"/> in your Startup/Program.cs to register all Interlocutor handlers, behaviors, and the mediator itself.
/// Assembly scanning discovers implementations of <see cref="IQueryHandler{TQuery, TResponse}"/>,
/// <see cref="ICommandHandler{TCommand, TResponse}"/>, <see cref="IVoidCommandHandler{TCommand, TResult}"/>, and
/// <see cref="INotificationHandler{TNotification}"/> without requiring explicit registrations for each handler.
/// </para>
/// </remarks>
public static class InterlocutorServiceCollectionExtensions
{
    /// <summary>
    /// Scans the specified assemblies for Interlocutor handler implementations and registers them in the service collection.
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
    public static IServiceCollection AddInterlocutor(this IServiceCollection services, params Assembly[] assembliesToScan)
    {
        // Delegate discovery to the container-neutral scanner and translate each resulting
        // registration into Microsoft DI's own primitives.
        Apply(services, HandlerDiscovery.Discover(assembliesToScan));

        // Register the mediator as the implementation for all three interfaces. ISender and IPublisher
        // are factory-registered to return the same IMediator instance, allowing callers to inject
        // any of the three interfaces and always get the same mediator.
        services.AddScoped<IMediator, Mediator>();
        services.AddScoped<ISender>(sp => sp.GetRequiredService<IMediator>());
        services.AddScoped<IPublisher>(sp => sp.GetRequiredService<IMediator>());

        return services;
    }

    /// <summary>
    /// Scans the assembly containing the specified marker type for Interlocutor handlers.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to register services into.</param>
    /// <param name="markerType">A type whose containing assembly will be scanned for handler implementations.</param>
    /// <returns>The same <see cref="IServiceCollection"/> instance for chaining.</returns>
    public static IServiceCollection AddInterlocutor(this IServiceCollection services, Type markerType) =>
        services.AddInterlocutor(markerType.Assembly);

    /// <summary>
    /// Scans the entry assembly for Interlocutor handler implementations.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to register services into.</param>
    /// <returns>The same <see cref="IServiceCollection"/> instance for chaining.</returns>
    public static IServiceCollection AddInterlocutorFromEntryAssembly(this IServiceCollection services) =>
        services.AddInterlocutor(Assembly.GetEntryAssembly()!);

    /// <summary>
    /// Configures Interlocutor with full pipeline behavior support via <see cref="InterlocutorOptions"/>.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to register services into.</param>
    /// <param name="options">Configuration options for behavior registration and assembly scanning.</param>
    /// <returns>The same <see cref="IServiceCollection"/> instance for chaining.</returns>
    /// <remarks>
    /// <para>
    /// This overload registers handlers, the mediator, and the complete pipeline behavior chain
    /// in the canonical documented order: Logging → Auditing → Authorization → Validation → Transaction.
    /// It also registers the <see cref="IResponseOutcomeDescriber"/> for behavior introspection.
    /// </para>
    /// <para>
    /// It automatically registers default implementations for behaviors' dependencies:
    /// <see cref="LoggingAuditSink"/> for <see cref="IAuditSink"/>,
    /// <see cref="DefaultPolicyEvaluator"/> for <see cref="IPolicyEvaluator"/>,
    /// and <see cref="AmbientRequestContextAccessor"/> for <see cref="IRequestContextAccessor"/>.
    /// </para>
    /// <para>
    /// If <see cref="InterlocutorOptions.EnableDefaultBehaviors"/> is true, the canonical behavior
    /// chain is registered. If the required packages are not loaded, behavior registration is skipped
    /// with a graceful fallback.
    /// </para>
    /// </remarks>
    public static IServiceCollection AddInterlocutor(this IServiceCollection services, InterlocutorOptions options)
    {
        // Apply assembly filter if provided
        var assembliesToScan =
            options.AssemblyFilter != null
                ? options.AssembliesToScan.Where(options.AssemblyFilter).ToArray()
                : options.AssembliesToScan;

        // Register handlers first using the base method
        services.AddInterlocutor(assembliesToScan);

        // Build and register the outcome classifier with custom types
        var classifierBuilder = new OutcomeClassifierBuilder();
        foreach (var customType in options.CustomOutcomeTypes)
        {
            // Attempt to register custom types as failures (conservative default)
            classifierBuilder.RegisterFailureType(customType, customType.Name);
        }

        services.AddSingleton(classifierBuilder.Build());

        // Register outcome describer with the classifier
        services.AddSingleton<IResponseOutcomeDescriber>(sp => new DefaultResponseOutcomeDescriber(
            sp.GetRequiredService<IOutcomeClassifier>()
        ));

        // Register default implementations for pipeline dependencies (always)
        Apply(services, DefaultPipelineRegistrations.Dependencies());

        // Register default behavior chain if enabled
        if (options.EnableDefaultBehaviors)
        {
            Apply(services, DefaultPipelineRegistrations.Behaviors());
        }

        return services;
    }

    /// <summary>
    /// Configures Interlocutor via the container-neutral <see cref="InterlocutorBuilder"/> — the single
    /// fluent configuration surface identical across every supported container. Registers the mediator,
    /// discovered handlers, and stages unconditionally; registers zero pipeline behaviors unless
    /// <paramref name="configure"/> adds them (see ADR-0009, "No Default Pipeline").
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to register services into.</param>
    /// <param name="configure">
    /// A delegate that configures assembly scanning, behaviors, stages, and default overrides on a fresh
    /// <see cref="InterlocutorBuilder"/>. The exact same delegate body works unchanged against
    /// <c>Interlocutor.DependencyInjection.Autofac</c>'s entry point.
    /// </param>
    /// <returns>The same <see cref="IServiceCollection"/> instance for chaining.</returns>
    /// <remarks>
    /// <para>
    /// This overload is additive alongside the legacy <see cref="AddInterlocutor(IServiceCollection, InterlocutorOptions)"/>
    /// overload — both remain available. <see cref="InterlocutorBuilder.Defaults"/> entries are translated
    /// with try-add semantics (see <see cref="TryApplyDefaults"/>), so a consumer's own registration of
    /// <see cref="IAuditSink"/>, <see cref="IPolicyEvaluator"/>, <see cref="IRequestContextAccessor"/>, or
    /// <see cref="IResponseOutcomeDescriber"/> — made before this call — always wins; the builder's own
    /// <c>Override*</c> methods cover winning when that registration is made after.
    /// </para>
    /// </remarks>
    public static IServiceCollection AddInterlocutor(this IServiceCollection services, Action<InterlocutorBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        var builder = new InterlocutorBuilder();
        configure(builder);

        Apply(services, HandlerDiscovery.Discover(builder.AssembliesToScan));
        Apply(services, builder.Stages);
        Apply(services, builder.Behaviors);
        TryApplyDefaults(services, builder.Defaults);

        services.AddScoped<IMediator, Mediator>();
        services.AddScoped<ISender>(sp => sp.GetRequiredService<IMediator>());
        services.AddScoped<IPublisher>(sp => sp.GetRequiredService<IMediator>());

        return services;
    }

    /// <summary>
    /// Translates a sequence of container-neutral <see cref="InterlocutorRegistration"/> records into
    /// Microsoft DI service descriptors.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to register services into.</param>
    /// <param name="registrations">The container-neutral registrations to translate.</param>
    private static void Apply(IServiceCollection services, IEnumerable<InterlocutorRegistration> registrations)
    {
        foreach (var registration in registrations)
        {
            switch (registration.Lifetime)
            {
                case RegistrationLifetime.Singleton:
                    services.AddSingleton(registration.ServiceType, registration.ImplementationType);
                    break;
                case RegistrationLifetime.Transient:
                    services.AddTransient(registration.ServiceType, registration.ImplementationType);
                    break;
                case RegistrationLifetime.Scoped:
                default:
                    services.AddScoped(registration.ServiceType, registration.ImplementationType);
                    break;
            }
        }
    }

    /// <summary>
    /// Translates <see cref="InterlocutorBuilder.Defaults"/> registrations into Microsoft DI's <c>TryAdd</c>
    /// descriptors, so an application's own registration of the same service type — made before this call
    /// runs — is left untouched rather than superseded.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to register services into.</param>
    /// <param name="defaults">The try-add-able default registrations to translate.</param>
    private static void TryApplyDefaults(IServiceCollection services, IEnumerable<InterlocutorRegistration> defaults)
    {
        foreach (var registration in defaults)
        {
            var descriptor = registration.Lifetime switch
            {
                RegistrationLifetime.Singleton => ServiceDescriptor.Singleton(registration.ServiceType, registration.ImplementationType),
                RegistrationLifetime.Transient => ServiceDescriptor.Transient(registration.ServiceType, registration.ImplementationType),
                RegistrationLifetime.Scoped or _ => ServiceDescriptor.Scoped(registration.ServiceType, registration.ImplementationType),
            };

            services.TryAdd(descriptor);
        }
    }
}
