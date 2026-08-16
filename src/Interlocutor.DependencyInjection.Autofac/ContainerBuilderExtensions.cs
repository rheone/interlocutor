namespace Interlocutor.DependencyInjection.Autofac;

using global::Autofac;
using global::Autofac.Builder;
using global::Autofac.Extensions.DependencyInjection;
using Interlocutor.Abstractions;
using Interlocutor.Abstractions.Commands;
using Interlocutor.Abstractions.Core;
using Interlocutor.Abstractions.Notifications;
using Interlocutor.Abstractions.Pipeline;
using Interlocutor.Abstractions.Queries;
using Interlocutor.DependencyInjection.Abstractions;

/// <summary>
/// Provides the extension method for registering Interlocutor services with an Autofac <see cref="ContainerBuilder"/>.
/// </summary>
/// <remarks>
/// <para>
/// Use <see cref="AddInterlocutor(ContainerBuilder, Action{InterlocutorBuilder})"/> in your Autofac module to
/// register the mediator, discovered handlers, and stages, and to configure any pipeline behaviors via the
/// container-neutral <see cref="InterlocutorBuilder"/>. Assembly scanning discovers handler implementations
/// without explicit registration for each handler type.
/// </para>
/// <para>
/// Unlike Microsoft DI (where handlers are scoped), Autofac handlers are registered as <c>InstancePerLifetimeScope</c>.
/// </para>
/// <para>
/// Per ADR-0009 ("No Default Pipeline"), a bare builder configures zero pipeline behaviors — nothing
/// cross-cutting runs unless the configuration delegate explicitly adds it.
/// </para>
/// </remarks>
public static class InterlocutorAutofacExtensions
{
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
    /// This is the only <c>AddInterlocutor</c> entry point Autofac exposes — there is no legacy
    /// options-based overload, no bare assembly-scanning overload, and no entry-assembly convenience
    /// overload; all configuration flows through <paramref name="configure"/>.
    /// <see cref="InterlocutorBuilder.Defaults"/> entries are translated with Autofac's <c>IfNotRegistered</c>
    /// try-add equivalent (see <see cref="TryApplyDefaults"/>), so a consumer's own registration of
    /// <see cref="IAuditSink"/>, <see cref="IPolicyEvaluator"/>, <see cref="IRequestContextAccessor"/>, or
    /// <see cref="IResponseOutcomeDescriber"/> — made before this call — always wins; the builder's own
    /// <c>Override*</c> methods cover winning when that registration is made after.
    /// </para>
    /// </remarks>
    public static ContainerBuilder AddInterlocutor(this ContainerBuilder builder, Action<InterlocutorBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        var interlocutorBuilder = new InterlocutorBuilder();
        configure(interlocutorBuilder);

        Apply(builder, HandlerDiscovery.Discover(interlocutorBuilder.AssembliesToScan));
        Apply(builder, interlocutorBuilder.GeneratedRegistrations);
        Apply(builder, interlocutorBuilder.Stages);
        ApplyFactories(builder, interlocutorBuilder.StageFactories);
        Apply(builder, interlocutorBuilder.Behaviors);
        TryApplyDefaults(builder, interlocutorBuilder.Defaults);

        // Bridge Autofac's lifetime scope to IServiceProvider. The Mediator uses IServiceProvider to resolve
        // handlers, so we need to adapt Autofac's ILifetimeScope to the standard IServiceProvider abstraction.
        // ExternallyOwned() tells Autofac not to manage the provider's lifetime since it's used transitively.
        builder
            .Register(c => new AutofacServiceProvider(c.Resolve<ILifetimeScope>()))
            .As<IServiceProvider>()
            .ExternallyOwned();

        // The real dispatcher is registered under its concrete type, not under IMediator directly, so the
        // root-scope guard (RootScopeGuardedMediator) can wrap it — see ADR-0011. Both the detector and the
        // guarded mediator are InstancePerLifetimeScope so a fresh answer/instance is produced per scope,
        // matching the concrete Mediator's own lifetime.
        builder.RegisterType<Mediator>().AsSelf().InstancePerLifetimeScope();
        builder.RegisterType<AutofacRootScopeDetector>().As<IRootScopeDetector>().InstancePerLifetimeScope();

        // Register RootScopeGuardedMediator as the implementation for all three interfaces (IMediator,
        // ISender, IPublisher). All three references resolve to the same instance, allowing callers to
        // inject any of them and get consistent behavior.
        builder
            .Register(c => new RootScopeGuardedMediator(c.Resolve<Mediator>(), c.Resolve<IRootScopeDetector>()))
            .As<IMediator>()
            .As<ISender>()
            .As<IPublisher>()
            .InstancePerLifetimeScope();

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
    /// Translates a sequence of container-neutral <see cref="InterlocutorFactoryRegistration"/> records —
    /// delegate-backed registrations such as lambda-form pipeline stages — into Autofac factory
    /// registrations, resolving the bridged <see cref="IServiceProvider"/> lazily at activation time.
    /// </summary>
    /// <param name="builder">The Autofac <see cref="ContainerBuilder"/> to register services into.</param>
    /// <param name="registrations">The container-neutral factory registrations to translate.</param>
    private static void ApplyFactories(ContainerBuilder builder, IEnumerable<InterlocutorFactoryRegistration> registrations)
    {
        foreach (var registration in registrations)
        {
            ApplyLifetime(
                builder.Register(c => registration.Factory(c.Resolve<IServiceProvider>())).As(registration.ServiceType),
                registration.Lifetime
            );
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
