namespace Interlocutor.DependencyInjection.Autofac;

using System.Reflection;
using global::Autofac;
using global::Autofac.Builder;
using global::Autofac.Extensions.DependencyInjection;
using Interlocutor.Abstractions;
using Interlocutor.Abstractions.Commands;
using Interlocutor.Abstractions.Core;
using Interlocutor.Abstractions.Notifications;
using Interlocutor.Abstractions.Pipeline;
using Interlocutor.Abstractions.Pipeline.Classifiers;
using Interlocutor.Abstractions.Queries;
using Interlocutor.DependencyInjection.Abstractions;

/// <summary>
/// Provides extension methods for registering Interlocutor services with an Autofac <see cref="ContainerBuilder"/>.
/// </summary>
/// <remarks>
/// <para>
/// Use the <c>AddInterlocutor</c> method in your Autofac module to register all Interlocutor handlers, behaviors, and the mediator.
/// Assembly scanning discovers handler implementations without explicit registration for each handler type.
/// </para>
/// <para>
/// Unlike Microsoft DI (where handlers are scoped), Autofac handlers are registered as <c>InstancePerLifetimeScope</c>.
/// </para>
/// </remarks>
public static class InterlocutorAutofacExtensions
{
    /// <summary>
    /// Scans the specified assemblies for Interlocutor handler implementations and registers them in the container.
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
    public static ContainerBuilder AddInterlocutor(this ContainerBuilder builder, params Assembly[] assembliesToScan)
    {
        // Delegate discovery to the container-neutral scanner and translate each resulting
        // registration into Autofac's own primitives.
        Apply(builder, HandlerDiscovery.Discover(assembliesToScan));

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
    /// Scans the assembly containing the specified marker type for Interlocutor handlers.
    /// </summary>
    /// <param name="builder">The Autofac <see cref="ContainerBuilder"/> to register services into.</param>
    /// <param name="markerType">A type whose containing assembly will be scanned for handler implementations.</param>
    /// <returns>The same <see cref="ContainerBuilder"/> instance for chaining.</returns>
    public static ContainerBuilder AddInterlocutor(this ContainerBuilder builder, Type markerType) =>
        builder.AddInterlocutor(markerType.Assembly);

    /// <summary>
    /// Scans the entry assembly for Interlocutor handler implementations.
    /// </summary>
    /// <param name="builder">The Autofac <see cref="ContainerBuilder"/> to register services into.</param>
    /// <returns>The same <see cref="ContainerBuilder"/> instance for chaining.</returns>
    public static ContainerBuilder AddInterlocutorFromEntryAssembly(this ContainerBuilder builder) =>
        builder.AddInterlocutor(Assembly.GetEntryAssembly()!);

    /// <summary>
    /// Configures Interlocutor with full pipeline behavior support via <see cref="InterlocutorOptions"/>.
    /// </summary>
    /// <param name="builder">The Autofac <see cref="ContainerBuilder"/> to register services into.</param>
    /// <param name="options">Configuration options for behavior registration and assembly scanning.</param>
    /// <returns>The same <see cref="ContainerBuilder"/> instance for chaining.</returns>
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
    public static ContainerBuilder AddInterlocutor(this ContainerBuilder builder, InterlocutorOptions options)
    {
        // Apply assembly filter if provided
        var assembliesToScan =
            options.AssemblyFilter != null
                ? options.AssembliesToScan.Where(options.AssemblyFilter).ToArray()
                : options.AssembliesToScan;

        // Register handlers first using the base method
        builder.AddInterlocutor(assembliesToScan);

        // Build and register the outcome classifier with custom types
        var classifierBuilder = new OutcomeClassifierBuilder();
        foreach (var customType in options.CustomOutcomeTypes)
        {
            // Attempt to register custom types as failures (conservative default)
            classifierBuilder.RegisterFailureType(customType, customType.Name);
        }

        builder.Register(c => classifierBuilder.Build()).As<IOutcomeClassifier>().SingleInstance();

        // Register outcome describer with the classifier
        builder
            .Register(c => new DefaultResponseOutcomeDescriber(c.Resolve<IOutcomeClassifier>()))
            .As<IResponseOutcomeDescriber>()
            .SingleInstance();

        // Register default implementations for pipeline dependencies (always)
        Apply(builder, DefaultPipelineRegistrations.Dependencies());

        // Register default behavior chain if enabled
        if (options.EnableDefaultBehaviors)
        {
            Apply(builder, DefaultPipelineRegistrations.Behaviors());
        }

        return builder;
    }

    /// <summary>
    /// Configures Interlocutor via the container-neutral <see cref="InterlocutorBuilder"/> — the single
    /// fluent configuration surface identical across every supported container. Registers the mediator,
    /// discovered handlers, and stages unconditionally; registers zero pipeline behaviors unless
    /// <paramref name="configure"/> adds them (see ADR-0009, "No Default Pipeline").
    /// </summary>
    /// <param name="builder">The Autofac <see cref="ContainerBuilder"/> to register services into.</param>
    /// <param name="configure">
    /// A delegate that configures assembly scanning, behaviors, stages, and default overrides on a fresh
    /// <see cref="InterlocutorBuilder"/>. The exact same delegate body works unchanged against
    /// <c>Interlocutor.DependencyInjection.Microsoft</c>'s entry point.
    /// </param>
    /// <returns>The same <see cref="ContainerBuilder"/> instance for chaining.</returns>
    /// <remarks>
    /// <para>
    /// This overload is additive alongside the legacy <see cref="AddInterlocutor(ContainerBuilder, InterlocutorOptions)"/>
    /// overload — both remain available. <see cref="InterlocutorBuilder.Defaults"/> entries are translated
    /// with Autofac's <c>IfNotRegistered</c> try-add equivalent (see <see cref="TryApplyDefaults"/>), so a
    /// consumer's own registration of <see cref="IAuditSink"/>, <see cref="IPolicyEvaluator"/>,
    /// <see cref="IRequestContextAccessor"/>, or <see cref="IResponseOutcomeDescriber"/> — made before this
    /// call — always wins; the builder's own <c>Override*</c> methods cover winning when that registration
    /// is made after.
    /// </para>
    /// </remarks>
    public static ContainerBuilder AddInterlocutor(this ContainerBuilder builder, Action<InterlocutorBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        var interlocutorBuilder = new InterlocutorBuilder();
        configure(interlocutorBuilder);

        Apply(builder, HandlerDiscovery.Discover(interlocutorBuilder.AssembliesToScan));
        Apply(builder, interlocutorBuilder.Stages);
        Apply(builder, interlocutorBuilder.Behaviors);
        TryApplyDefaults(builder, interlocutorBuilder.Defaults);

        builder
            .Register(c => new AutofacServiceProvider(c.Resolve<ILifetimeScope>()))
            .As<IServiceProvider>()
            .ExternallyOwned();

        builder.RegisterType<Mediator>().As<IMediator>().As<ISender>().As<IPublisher>().InstancePerLifetimeScope();

        return builder;
    }

    /// <summary>
    /// Translates a sequence of container-neutral <see cref="InterlocutorRegistration"/> records into
    /// Autofac registrations, using <c>InstancePerLifetimeScope</c> as the Autofac equivalent of
    /// <see cref="RegistrationLifetime.Scoped"/>.
    /// </summary>
    /// <param name="builder">The Autofac <see cref="ContainerBuilder"/> to register services into.</param>
    /// <param name="registrations">The container-neutral registrations to translate.</param>
    private static void Apply(ContainerBuilder builder, IEnumerable<InterlocutorRegistration> registrations)
    {
        foreach (var registration in registrations)
        {
            // Open generic implementation types (e.g. IPipelineBehavior<,> behaviors) require
            // RegisterGeneric; closed types (e.g. discovered handlers) require RegisterType.
            if (registration.ImplementationType.IsGenericTypeDefinition)
            {
                ApplyLifetime(
                    builder.RegisterGeneric(registration.ImplementationType).As(registration.ServiceType),
                    registration.Lifetime
                );
            }
            else
            {
                ApplyLifetime(
                    builder.RegisterType(registration.ImplementationType).As(registration.ServiceType),
                    registration.Lifetime
                );
            }
        }
    }

    /// <summary>
    /// Translates <see cref="InterlocutorBuilder.Defaults"/> registrations into Autofac registrations
    /// guarded by <c>IfNotRegistered</c> — Autofac's try-add equivalent — so an application's own
    /// registration of the same service type, made before this call runs, is left untouched.
    /// </summary>
    /// <param name="builder">The Autofac <see cref="ContainerBuilder"/> to register services into.</param>
    /// <param name="defaults">The try-add-able default registrations to translate.</param>
    private static void TryApplyDefaults(ContainerBuilder builder, IEnumerable<InterlocutorRegistration> defaults)
    {
        foreach (var registration in defaults)
        {
            if (registration.ImplementationType.IsGenericTypeDefinition)
            {
                ApplyLifetime(
                    builder
                        .RegisterGeneric(registration.ImplementationType)
                        .As(registration.ServiceType)
                        .IfNotRegistered(registration.ServiceType),
                    registration.Lifetime
                );
            }
            else
            {
                ApplyLifetime(
                    builder
                        .RegisterType(registration.ImplementationType)
                        .As(registration.ServiceType)
                        .IfNotRegistered(registration.ServiceType),
                    registration.Lifetime
                );
            }
        }
    }

    /// <summary>
    /// Applies the Autofac scope method corresponding to a container-neutral <see cref="RegistrationLifetime"/>.
    /// </summary>
    /// <param name="registrationBuilder">The in-progress Autofac registration to configure.</param>
    /// <param name="lifetime">The container-neutral lifetime to translate.</param>
    private static void ApplyLifetime<TActivatorData, TRegistrationStyle>(
        IRegistrationBuilder<object, TActivatorData, TRegistrationStyle> registrationBuilder,
        RegistrationLifetime lifetime
    )
    {
        switch (lifetime)
        {
            case RegistrationLifetime.Singleton:
                registrationBuilder.SingleInstance();
                break;
            case RegistrationLifetime.Transient:
                registrationBuilder.InstancePerDependency();
                break;
            case RegistrationLifetime.Scoped:
            default:
                registrationBuilder.InstancePerLifetimeScope();
                break;
        }
    }
}
