namespace Interlocutor.DependencyInjection.Microsoft;

using global::Microsoft.Extensions.DependencyInjection;
using global::Microsoft.Extensions.DependencyInjection.Extensions;
using global::Microsoft.Extensions.Logging;
using Interlocutor.Abstractions;
using Interlocutor.Abstractions.Commands;
using Interlocutor.Abstractions.Core;
using Interlocutor.Abstractions.Notifications;
using Interlocutor.Abstractions.Pipeline;
using Interlocutor.Abstractions.Queries;
using Interlocutor.DependencyInjection.Abstractions;

/// <summary>
/// Provides the extension method for registering Interlocutor services with a Microsoft DI <see cref="IServiceCollection"/>.
/// </summary>
/// <remarks>
/// <para>
/// Use <see cref="AddInterlocutor(IServiceCollection, Action{InterlocutorBuilder})"/> in your Startup/Program.cs to
/// register the mediator, discovered handlers, and stages, and to configure any pipeline behaviors via the
/// container-neutral <see cref="InterlocutorBuilder"/>. Assembly scanning discovers implementations of
/// <see cref="IQueryHandler{TQuery, TResponse}"/>, <see cref="ICommandHandler{TCommand, TResponse}"/>,
/// <see cref="IVoidCommandHandler{TCommand, TResult}"/>, and <see cref="INotificationHandler{TNotification}"/>
/// without requiring explicit registrations for each handler.
/// </para>
/// <para>
/// Per ADR-0009 ("No Default Pipeline"), a bare builder configures zero pipeline behaviors - nothing
/// cross-cutting runs unless the <c>configure</c> delegate explicitly adds it.
/// </para>
/// </remarks>
public static class InterlocutorServiceCollectionExtensions
{
    /// <summary>
    /// Configures Interlocutor via the container-neutral <see cref="InterlocutorBuilder"/> - the single
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
    /// This is the only <c>AddInterlocutor</c> entry point Microsoft DI exposes - there is no legacy
    /// options-based overload, no bare assembly-scanning overload, and no entry-assembly convenience
    /// overload; all configuration flows through <paramref name="configure"/>.
    /// <see cref="InterlocutorBuilder.Defaults"/> entries are translated with try-add semantics (see
    /// <see cref="TryApplyDefaults"/>), so a consumer's own registration of <see cref="IAuditSink"/>,
    /// <see cref="IPolicyEvaluator"/>, <see cref="IRequestContextAccessor"/>, or
    /// <see cref="IResponseOutcomeDescriber"/> - made before this call - always wins; the builder's own
    /// <c>Override*</c> methods cover winning when that registration is made after.
    /// </para>
    /// </remarks>
    public static IServiceCollection AddInterlocutor(this IServiceCollection services, Action<InterlocutorBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        var builder = new InterlocutorBuilder();
        configure(builder);

        // Built once and reused for validation, the optional log below, and nothing else - it needs no
        // live container, so it is checked before this call mutates services at all: a caller catching
        // InterlocutorRegistrationValidationException finds services exactly as it left them, not partially
        // configured. Reused rather than calling DescribeRegistrations() a second time, since it re-scans
        // every configured assembly each time.
        var report = builder.DescribeRegistrations();
        if (report.Problems.Count > 0)
        {
            throw new InterlocutorRegistrationValidationException(report);
        }

        Apply(services, builder.Handlers);
        Apply(services, builder.GeneratedRegistrations);
        Apply(services, builder.Stages);
        ApplyFactories(services, builder.StageFactories);
        Apply(services, builder.Behaviors);
        Apply(services, builder.MatchedRegistrations);
        TryApplyDefaults(services, builder.Defaults);
        TryLogRegistrationReport(services, report);

        // The real dispatcher is registered under its concrete type, not under IMediator directly, so the
        // root-scope guard (RootScopeGuardedMediator) can wrap it - see ADR-0011. RootServiceProviderMarker
        // is a singleton (see its own remarks for why that makes it a stable root reference); the detector
        // and the guarded mediator are scoped, so both are recomputed per scope alongside everything else the
        // dispatch chain resolves.
        services.AddSingleton<RootServiceProviderMarker>();
        services.AddScoped<IRootScopeDetector, MicrosoftRootScopeDetector>();
        services.AddScoped<Mediator>();
        services.AddScoped<IMediator>(sp => new RootScopeGuardedMediator(
            sp.GetRequiredService<Mediator>(),
            sp.GetRequiredService<IRootScopeDetector>()
        ));
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
            var serviceType = ResolveServiceType(registration);
            switch (registration.Lifetime)
            {
                case RegistrationLifetime.Singleton:
                    services.AddSingleton(serviceType, registration.ImplementationType);
                    break;
                case RegistrationLifetime.Transient:
                    services.AddTransient(serviceType, registration.ImplementationType);
                    break;
                case RegistrationLifetime.Scoped:
                default:
                    services.AddScoped(serviceType, registration.ImplementationType);
                    break;
            }
        }
    }

    /// <summary>
    /// Resolves the service type a registration should actually be paired with under Microsoft DI, which -
    /// unlike Autofac - requires an open generic service type to be paired with an open generic
    /// implementation type. <see cref="InterlocutorBuilder.AddBehavior"/> always records the open
    /// <see cref="IPipelineBehavior{TRequest,TResponse}"/> as <see cref="InterlocutorRegistration.ServiceType"/>
    /// regardless of whether <see cref="InterlocutorRegistration.ImplementationType"/> is itself open or
    /// closed (a legal shape - see that method's remarks), so a closed implementation type here is
    /// re-paired with the specific closed <see cref="IPipelineBehavior{TRequest,TResponse}"/> interface it
    /// actually implements. Mirrors the open/closed branch <c>ContainerBuilderExtensions.Apply</c> already
    /// has for Autofac, which needs no such translation because Autofac accepts the open/closed mismatch directly.
    /// </summary>
    /// <param name="registration">The registration to resolve the effective service type for.</param>
    /// <returns>
    /// The closed interface <see cref="InterlocutorRegistration.ImplementationType"/> implements, when
    /// <see cref="InterlocutorRegistration.ServiceType"/> is an open generic definition and
    /// <see cref="InterlocutorRegistration.ImplementationType"/> is closed; otherwise, the registration's
    /// own <see cref="InterlocutorRegistration.ServiceType"/> unchanged.
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
    /// Translates a sequence of container-neutral <see cref="InterlocutorFactoryRegistration"/> records -
    /// delegate-backed registrations such as lambda-form pipeline stages - into Microsoft DI factory
    /// service descriptors.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to register services into.</param>
    /// <param name="registrations">The container-neutral factory registrations to translate.</param>
    private static void ApplyFactories(IServiceCollection services, IEnumerable<InterlocutorFactoryRegistration> registrations)
    {
        foreach (var registration in registrations)
        {
            switch (registration.Lifetime)
            {
                case RegistrationLifetime.Singleton:
                    services.AddSingleton(registration.ServiceType, registration.Factory);
                    break;
                case RegistrationLifetime.Transient:
                    services.AddTransient(registration.ServiceType, registration.Factory);
                    break;
                case RegistrationLifetime.Scoped:
                default:
                    services.AddScoped(registration.ServiceType, registration.Factory);
                    break;
            }
        }
    }

    /// <summary>
    /// Translates <see cref="InterlocutorBuilder.Defaults"/> registrations into Microsoft DI's <c>TryAdd</c>
    /// descriptors, so an application's own registration of the same service type - made before this call
    /// runs - is left untouched rather than superseded.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to register services into.</param>
    /// <param name="defaults">The try-add-able default registrations to translate.</param>
    private static void TryApplyDefaults(IServiceCollection services, IEnumerable<InterlocutorRegistration> defaults)
    {
        foreach (var registration in defaults)
        {
            var descriptor = registration.Lifetime switch
            {
                RegistrationLifetime.Singleton => ServiceDescriptor.Singleton(
                    registration.ServiceType,
                    registration.ImplementationType
                ),
                RegistrationLifetime.Transient => ServiceDescriptor.Transient(
                    registration.ServiceType,
                    registration.ImplementationType
                ),
                RegistrationLifetime.Scoped or _ => ServiceDescriptor.Scoped(
                    registration.ServiceType,
                    registration.ImplementationType
                ),
            };

            services.TryAdd(descriptor);
        }
    }

    /// <summary>
    /// Logs <see cref="InterlocutorBuilder.DescribeRegistrations"/>'s formatted report at
    /// <c>AddInterlocutor()</c> time, but only when the application has already registered
    /// <see cref="ILoggerFactory"/> in <paramref name="services"/> - additive, never required to use the
    /// report. Absent that, or if resolving a logger from the services registered so far fails for any
    /// reason, this is silently skipped: it is observability, not a configuration requirement, and must
    /// never turn a working <c>AddInterlocutor()</c> call into a failing one.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> being configured.</param>
    /// <param name="report">The already-built report to log.</param>
    private static void TryLogRegistrationReport(IServiceCollection services, RegistrationReport report)
    {
        if (!services.Any(descriptor => descriptor.ServiceType == typeof(ILoggerFactory)))
        {
            return;
        }

        try
        {
            using var loggingOnlyProvider = services.BuildServiceProvider(
                new ServiceProviderOptions { ValidateOnBuild = false, ValidateScopes = false }
            );
            var logger = loggingOnlyProvider.GetRequiredService<ILoggerFactory>().CreateLogger("Interlocutor");
            logger.LogInformation("{Report}", report.Format());
        }
        catch (InvalidOperationException)
        {
            // The application's own logging setup could not be resolved from what is registered so far
            // (for example, a dependency it needs is registered after this AddInterlocutor() call).
            // Reporting is advisory only - leave AddInterlocutor() itself unaffected.
        }
    }
}
