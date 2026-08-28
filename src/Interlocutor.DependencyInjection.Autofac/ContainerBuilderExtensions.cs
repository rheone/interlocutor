namespace Interlocutor.DependencyInjection.Autofac;

using System.Diagnostics.CodeAnalysis;
using global::Autofac;
using global::Autofac.Builder;
using global::Autofac.Extensions.DependencyInjection;
using global::Microsoft.Extensions.Logging;
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
/// Per ADR-0009 ("No Default Pipeline"), a bare builder configures zero pipeline behaviors - nothing
/// cross-cutting runs unless the configuration delegate explicitly adds it.
/// </para>
/// </remarks>
public static class InterlocutorAutofacExtensions
{
    /// <summary>
    /// Configures Interlocutor via the container-neutral <see cref="InterlocutorBuilder"/> - the single
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
    /// This is the only <c>AddInterlocutor</c> entry point Autofac exposes - there is no legacy
    /// options-based overload, no bare assembly-scanning overload, and no entry-assembly convenience
    /// overload; all configuration flows through <paramref name="configure"/>.
    /// <see cref="InterlocutorBuilder.Defaults"/> entries are translated with Autofac's <c>IfNotRegistered</c>
    /// try-add equivalent (see <see cref="Translate"/>), so a consumer's own registration of
    /// <see cref="IAuditSink"/>, <see cref="IPolicyEvaluator"/>, <see cref="IRequestContextAccessor"/>, or
    /// <see cref="IResponseOutcomeDescriber"/> - made before this call - always wins; the builder's own
    /// <c>Override*</c> methods cover winning when that registration is made after.
    /// </para>
    /// </remarks>
    public static ContainerBuilder AddInterlocutor(this ContainerBuilder builder, Action<InterlocutorBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        var interlocutorBuilder = new InterlocutorBuilder();
        configure(interlocutorBuilder);

        // Built once and reused for validation, the deferred log below, and nothing else - it needs no
        // live container, so it is checked before this call mutates builder at all: a caller catching
        // InterlocutorRegistrationValidationException finds builder exactly as it left it, not partially
        // configured. Reused rather than calling DescribeRegistrations() a second time, since it re-scans
        // every configured assembly each time.
        var report = interlocutorBuilder.DescribeRegistrations();
        if (report.Problems.Count > 0)
        {
            throw new InterlocutorRegistrationValidationException(report);
        }

        var applier = new RegistrationApplier(
            addRegistration: registration => Translate(builder, registration, tryAdd: false),
            addFactory: registration =>
                SetLifetime(
                    builder.Register(c => registration.Factory(c.Resolve<IServiceProvider>())).As(registration.ServiceType),
                    registration.Lifetime
                ),
            tryAddDefault: registration => Translate(builder, registration, tryAdd: true)
        );
        applier.Apply(interlocutorBuilder);

        // Deferred to a build callback (rather than resolved eagerly here) since only the fully-built
        // container can answer whether the application actually registered an ILoggerFactory - Autofac's
        // registration order is unordered relative to this call, unlike Microsoft DI's IServiceCollection,
        // which this same probe can inspect synchronously (see ServiceCollectionExtensions.TryLogRegistrationReport).
        builder.RegisterBuildCallback(container => TryLogRegistrationReport(container, report));

        // Bridge Autofac's lifetime scope to IServiceProvider. The Mediator uses IServiceProvider to resolve
        // handlers, so we need to adapt Autofac's ILifetimeScope to the standard IServiceProvider abstraction.
        // ExternallyOwned() tells Autofac not to manage the provider's lifetime since it's used transitively.
        builder
            .Register(c => new AutofacServiceProvider(c.Resolve<ILifetimeScope>()))
            .As<IServiceProvider>()
            .ExternallyOwned();

        // The real dispatcher is registered under its concrete type, not under IMediator directly, so the
        // root-scope guard (RootScopeGuardedMediator) can wrap it - see ADR-0011. Both the detector and the
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
    /// Translates one container-neutral <see cref="InterlocutorRegistration"/> into an Autofac registration -
    /// the single translation both the <see cref="RegistrationApplier"/> <c>addRegistration</c> and
    /// <c>tryAddDefault</c> callbacks share, differing only in whether Autofac's <c>IfNotRegistered</c>
    /// try-add guard applies.
    /// </summary>
    /// <param name="builder">The Autofac <see cref="ContainerBuilder"/> to register services into.</param>
    /// <param name="registration">The container-neutral registration to translate.</param>
    /// <param name="tryAdd">
    /// Whether to guard the registration with Autofac's <c>IfNotRegistered</c> - <see langword="true"/> for
    /// <see cref="InterlocutorBuilder.Defaults"/> entries, so a consumer's own registration of the same
    /// service type, made before this call, is left untouched.
    /// </param>
    private static void Translate(ContainerBuilder builder, InterlocutorRegistration registration, bool tryAdd)
    {
        // Open generic implementation types (e.g. IPipelineBehavior<,> behaviors) require
        // RegisterGeneric; closed types (e.g. discovered handlers) require RegisterType.
        if (registration.ImplementationType.IsGenericTypeDefinition)
        {
            var registrationBuilder = builder.RegisterGeneric(registration.ImplementationType).As(registration.ServiceType);
            SetLifetime(
                tryAdd ? registrationBuilder.IfNotRegistered(registration.ServiceType) : registrationBuilder,
                registration.Lifetime
            );
        }
        else
        {
            // The try-add (Defaults) path registers against the service type as-is - defaults never have
            // the closed-implementation-behind-open-service-type mismatch ResolveServiceType exists for.
            var serviceType = tryAdd ? registration.ServiceType : ResolveServiceType(registration);
            var registrationBuilder = builder.RegisterType(registration.ImplementationType).As(serviceType);
            SetLifetime(
                tryAdd ? registrationBuilder.IfNotRegistered(registration.ServiceType) : registrationBuilder,
                registration.Lifetime
            );
        }
    }

    /// <summary>
    /// Resolves the service type a registration should actually be paired with under Autofac. Unlike
    /// Microsoft DI, Autofac has no problem pairing an open generic implementation with an open generic
    /// service type via <c>RegisterGeneric(...).As(...)</c> - but the reverse, a <em>closed</em> implementation
    /// paired with an <em>open</em> service type via <c>RegisterType(...).As(...)</c>, throws
    /// <see cref="ArgumentException"/> at <c>ContainerBuilder.Build()</c> time, the same shape Microsoft
    /// DI's own <c>ServiceCollectionExtensions.ResolveServiceType</c> exists to handle. Both containers need
    /// this translation for the identical reason: <see cref="InterlocutorPipelineBuilder.AddBehavior"/> always
    /// records the open <see cref="IPipelineBehavior{TRequest,TResponse}"/> as
    /// <see cref="InterlocutorRegistration.ServiceType"/> regardless of whether
    /// <see cref="InterlocutorRegistration.ImplementationType"/> is itself open or closed (a legal shape - see
    /// that method's remarks), so a closed implementation type here is re-paired with the specific closed
    /// <see cref="IPipelineBehavior{TRequest,TResponse}"/> interface it actually implements.
    /// </summary>
    /// <param name="registration">The registration to resolve the effective service type for.</param>
    /// <returns>
    /// The closed interface <see cref="InterlocutorRegistration.ImplementationType"/> implements, when
    /// <see cref="InterlocutorRegistration.ServiceType"/> is an open generic definition and
    /// <see cref="InterlocutorRegistration.ImplementationType"/> is closed; otherwise, the registration's own
    /// <see cref="InterlocutorRegistration.ServiceType"/> unchanged.
    /// </returns>
    private static Type ResolveServiceType(InterlocutorRegistration registration)
    {
        var serviceType = registration.ServiceType;
        if (!serviceType.IsGenericTypeDefinition || registration.ImplementationType.IsGenericTypeDefinition)
        {
            return serviceType;
        }

        var closedServiceInterface = registration
            .ImplementationType.GetInterfaces()
            .FirstOrDefault(candidate => candidate.IsGenericType && candidate.GetGenericTypeDefinition() == serviceType);

        return closedServiceInterface ?? serviceType;
    }

    /// <summary>
    /// Logs <see cref="InterlocutorBuilder.DescribeRegistrations"/>'s formatted report once the container
    /// finishes building, but only when the application has registered <see cref="ILoggerFactory"/> -
    /// additive, never required to use the report. A container that never registered one is silently
    /// skipped: reporting is observability, not a configuration requirement.
    /// </summary>
    /// <param name="container">The fully-built root lifetime scope the build callback receives.</param>
    /// <param name="report">The already-built report to log.</param>
    private static void TryLogRegistrationReport(ILifetimeScope container, RegistrationReport report)
    {
        // CA2000 misreads this as a leak: loggerFactory is resolved from the container, not created by this
        // method, so the container - not this method - owns its lifetime and disposal.
#pragma warning disable CA2000
        if (!container.TryResolve<ILoggerFactory>(out var loggerFactory))
        {
            return;
        }
#pragma warning restore CA2000

        var logger = loggerFactory.CreateLogger("Interlocutor");
        logger.LogInformation("{Report}", report.Format());
    }

    /// <summary>
    /// Applies the Autofac scope method corresponding to a container-neutral <see cref="RegistrationLifetime"/>.
    /// </summary>
    /// <param name="registrationBuilder">The in-progress Autofac registration to configure.</param>
    /// <param name="lifetime">The container-neutral lifetime to translate.</param>
    private static void SetLifetime<TActivatorData, TRegistrationStyle>(
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
            default:
                registrationBuilder.InstancePerLifetimeScope();
                break;
        }
    }
}
