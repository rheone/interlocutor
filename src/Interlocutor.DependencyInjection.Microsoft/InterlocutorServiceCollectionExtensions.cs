namespace Interlocutor.DependencyInjection.Microsoft;

using System.Diagnostics.CodeAnalysis;
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
/// <see cref="IBareCommandHandler{TCommand, TResult}"/>, and <see cref="INotificationHandler{TNotification}"/>
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
    /// <see cref="InterlocutorBuilder.Defaults"/> entries are translated with try-add semantics (via the
    /// shared <see cref="RegistrationApplier"/>'s <c>tryAddDefault</c> callback), so a consumer's own
    /// registration of <see cref="IAuditSink"/>,
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

        var applier = new RegistrationApplier(
            addRegistration: registration =>
                services.Add(
                    CreateDescriptor(ResolveServiceType(registration), registration.ImplementationType, registration.Lifetime)
                ),
            addFactory: registration =>
                services.Add(CreateDescriptor(registration.ServiceType, registration.Factory, registration.Lifetime)),
            tryAddDefault: registration =>
                services.TryAdd(
                    CreateDescriptor(ResolveServiceType(registration), registration.ImplementationType, registration.Lifetime)
                )
        );
        applier.Apply(builder);
        TryLogRegistrationReport(services, report);

        // Registered unconditionally, even when no override rule was ever declared, so PipelineChainResolver
        // can resolve it without a null-check for "was AddInterlocutor used at all" - an empty rule set
        // simply never matches, which is exactly today's unaffected baseline behavior.
        services.AddSingleton<IPipelineOverrideRegistry>(new PipelineOverrideRegistry(builder.Pipeline.OverrideRules));

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
    /// Resolves the service type a registration should actually be paired with under Microsoft DI, which
    /// requires an open generic service type to be paired with an open generic implementation type.
    /// <see cref="InterlocutorPipelineBuilder.AddBehavior"/> always records the open
    /// <see cref="IPipelineBehavior{TRequest,TResponse}"/> as <see cref="InterlocutorRegistration.ServiceType"/>
    /// regardless of whether <see cref="InterlocutorRegistration.ImplementationType"/> is itself open or
    /// closed (a legal shape - see that method's remarks), so a closed implementation type here is
    /// re-paired with the specific closed <see cref="IPipelineBehavior{TRequest,TResponse}"/> interface it
    /// actually implements. Autofac needs the identical translation for the identical reason - see
    /// <c>ContainerBuilderExtensions.ResolveServiceType</c>: it too throws at build time for a closed
    /// implementation registered directly against an open service type.
    /// </summary>
    /// <param name="registration">The registration to resolve the effective service type for.</param>
    /// <returns>
    /// The closed interface <see cref="InterlocutorRegistration.ImplementationType"/> implements, when
    /// <see cref="InterlocutorRegistration.ServiceType"/> is an open generic definition and
    /// <see cref="InterlocutorRegistration.ImplementationType"/> is closed; otherwise, the registration's
    /// own <see cref="InterlocutorRegistration.ServiceType"/> unchanged.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// <see cref="InterlocutorRegistration.ServiceType"/> and <see cref="InterlocutorRegistration.ImplementationType"/>
    /// are both open generic definitions with a different number of type parameters - a shape
    /// Microsoft.Extensions.DependencyInjection cannot register at all (it requires an open generic
    /// implementation to declare exactly as many open type parameters as the service interface it's paired
    /// with). Thrown here, synchronously during <c>AddInterlocutor</c>, so the failure is immediate and
    /// actionable instead of a cryptic <see cref="ArgumentException"/> the framework would otherwise throw
    /// later, lazily, the first time <c>BuildServiceProvider()</c> runs. See
    /// <see cref="InterlocutorPipelineBuilder.AddUnitOfWorkBehavior"/>'s remarks for the concrete case this
    /// guards (EF Core's arity-3 <c>UnitOfWorkBehavior&lt;TRequest,TResponse,TContext&gt;</c> behind the
    /// arity-2 <see cref="IPipelineBehavior{TRequest,TResponse}"/>) and the documented workaround.
    /// </exception>
    private static Type ResolveServiceType(InterlocutorRegistration registration)
    {
        var serviceType = registration.ServiceType;
        var implementationType = registration.ImplementationType;
        if (!serviceType.IsGenericTypeDefinition)
        {
            return serviceType;
        }

        if (implementationType.IsGenericTypeDefinition)
        {
            var serviceArity = serviceType.GetGenericArguments().Length;
            var implementationArity = implementationType.GetGenericArguments().Length;
            if (serviceArity != implementationArity)
            {
                // The arity marker ("`3") isn't source syntax - strip it so the suggested subclass declaration
                // in the message reads as real C# the caller can paste, not raw reflection formatting.
                var implementationTypeBaseName = implementationType.Name.Split('`')[0];
                throw new InvalidOperationException(
                    $"Cannot register open generic type '{implementationType.Name}' ({implementationArity} type "
                        + $"parameter(s)) against open generic service '{serviceType.Name}' ({serviceArity} type "
                        + "parameter(s)) under Microsoft.Extensions.DependencyInjection: it requires an open "
                        + "generic implementation to declare exactly as many open type parameters as the service "
                        + "interface it's registered against. Define a subclass that fixes the extra type "
                        + "parameter(s) to a concrete type while leaving the rest open - for example, "
                        + $"`class My{implementationTypeBaseName}<TRequest,TResponse> : "
                        + $"{implementationTypeBaseName}<TRequest,TResponse,MyConcreteType>` - and register that "
                        + "subclass via InterlocutorPipelineBuilder.AddBehavior(typeof(...)) instead."
                );
            }

            return serviceType;
        }

        var closedServiceInterface = implementationType
            .GetInterfaces()
            .FirstOrDefault(candidate => candidate.IsGenericType && candidate.GetGenericTypeDefinition() == serviceType);

        return closedServiceInterface ?? serviceType;
    }

    /// <summary>Translates a container-neutral <see cref="RegistrationLifetime"/> into a <see cref="ServiceLifetime"/>.</summary>
    /// <param name="lifetime">The container-neutral lifetime to translate.</param>
    /// <returns>The equivalent <see cref="ServiceLifetime"/>.</returns>
    private static ServiceLifetime ToServiceLifetime(RegistrationLifetime lifetime) =>
        lifetime switch
        {
            RegistrationLifetime.Singleton => ServiceLifetime.Singleton,
            RegistrationLifetime.Transient => ServiceLifetime.Transient,
            RegistrationLifetime.Scoped or _ => ServiceLifetime.Scoped,
        };

    /// <summary>Builds a type-implementation <see cref="ServiceDescriptor"/> for the given container-neutral lifetime.</summary>
    /// <param name="serviceType">The service type to register.</param>
    /// <param name="implementationType">The implementation type to register.</param>
    /// <param name="lifetime">The container-neutral lifetime to translate.</param>
    /// <returns>The built <see cref="ServiceDescriptor"/>.</returns>
    private static ServiceDescriptor CreateDescriptor(
        Type serviceType,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type implementationType,
        RegistrationLifetime lifetime
    ) => new(serviceType, implementationType, ToServiceLifetime(lifetime));

    /// <summary>Builds a factory-backed <see cref="ServiceDescriptor"/> for the given container-neutral lifetime.</summary>
    /// <param name="serviceType">The service type to register.</param>
    /// <param name="factory">The factory that creates the service instance.</param>
    /// <param name="lifetime">The container-neutral lifetime to translate.</param>
    /// <returns>The built <see cref="ServiceDescriptor"/>.</returns>
    private static ServiceDescriptor CreateDescriptor(
        Type serviceType,
        Func<IServiceProvider, object> factory,
        RegistrationLifetime lifetime
    ) => new(serviceType, factory, ToServiceLifetime(lifetime));

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
