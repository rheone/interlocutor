namespace Interlocutor.DependencyInjection.Microsoft;

using global::Microsoft.Extensions.DependencyInjection;
using global::Microsoft.Extensions.DependencyInjection.Extensions;
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
/// Per ADR-0009 ("No Default Pipeline"), a bare builder configures zero pipeline behaviors — nothing
/// cross-cutting runs unless the <c>configure</c> delegate explicitly adds it.
/// </para>
/// </remarks>
public static class InterlocutorServiceCollectionExtensions
{
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
    /// This is the only <c>AddInterlocutor</c> entry point Microsoft DI exposes — there is no legacy
    /// options-based overload, no bare assembly-scanning overload, and no entry-assembly convenience
    /// overload; all configuration flows through <paramref name="configure"/>.
    /// <see cref="InterlocutorBuilder.Defaults"/> entries are translated with try-add semantics (see
    /// <see cref="TryApplyDefaults"/>), so a consumer's own registration of <see cref="IAuditSink"/>,
    /// <see cref="IPolicyEvaluator"/>, <see cref="IRequestContextAccessor"/>, or
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
        ApplyFactories(services, builder.StageFactories);
        Apply(services, builder.Behaviors);
        TryApplyDefaults(services, builder.Defaults);

        // The real dispatcher is registered under its concrete type, not under IMediator directly, so the
        // root-scope guard (RootScopeGuardedMediator) can wrap it — see ADR-0011. RootServiceProviderMarker
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
    /// Translates a sequence of container-neutral <see cref="InterlocutorFactoryRegistration"/> records —
    /// delegate-backed registrations such as lambda-form pipeline stages — into Microsoft DI factory
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
}
