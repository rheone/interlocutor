namespace Interlocutor.DependencyInjection.Microsoft;

using System.Reflection;
using global::Microsoft.Extensions.DependencyInjection;
using Interlocutor.Abstractions;
using Interlocutor.Abstractions.Commands;
using Interlocutor.Abstractions.Core;
using Interlocutor.Abstractions.Notifications;
using Interlocutor.Abstractions.Pipeline;
using Interlocutor.Abstractions.Pipeline.Classifiers;
using Interlocutor.Abstractions.Queries;

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
        // Scan assemblies for concrete, non-abstract types that might implement handler interfaces.
        var handlerTypes = assembliesToScan.SelectMany(a => a.GetTypes()).Where(t => t is { IsClass: true, IsAbstract: false });

        // Register each concrete handler against every matching Interlocutor handler interface.
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
        services.AddScoped<IAuditSink, LoggingAuditSink>();
        services.AddScoped<IPolicyEvaluator, DefaultPolicyEvaluator>();
        services.AddScoped<IRequestContextAccessor, AmbientRequestContextAccessor>();

        // Register default behavior chain if enabled
        if (options.EnableDefaultBehaviors)
        {
            RegisterDefaultBehaviors(services, assembliesToScan);
        }

        return services;
    }

    /// <summary>
    /// Registers the canonical pipeline behavior chain in documented order.
    /// </summary>
    private static void RegisterDefaultBehaviors(IServiceCollection services, Assembly[] assembliesToScan)
    {
        // Try to load and register each behavior. If a type doesn't exist in loaded assemblies,
        // that's ok — the consumer just won't have that behavior (e.g., if FluentValidation isn't referenced).

        var behaviorTypes = new (Type BehaviorType, Assembly? SourceAssembly)[]
        {
            // Logging outermost
            (typeof(LoggingBehavior<,>), typeof(LoggingBehavior<,>).Assembly),
            // Auditing after logging
            (typeof(AuditingBehavior<,>), typeof(AuditingBehavior<,>).Assembly),
            // Authorization before validation
            (typeof(AuthorizationBehavior<,>), typeof(AuthorizationBehavior<,>).Assembly),
            // Validation before transaction
            (TryGetType("Interlocutor.FluentValidation.ValidationBehavior`2"), null),
            // Transaction innermost
            (
                TryGetType("Interlocutor.EntityFrameworkCore.TransactionBehavior`3")
                    ?? TryGetType("Interlocutor.NHibernate.TransactionBehavior`3"),
                null
            ),
        };

        foreach (var (behaviorType, _) in behaviorTypes)
        {
            if (behaviorType != null)
            {
                services.AddScoped(typeof(IPipelineBehavior<,>), behaviorType);
            }
        }
    }

    /// <summary>
    /// Attempts to resolve a behavior type by name, returning null if not found.
    /// </summary>
    private static Type? TryGetType(string typeName)
    {
        try
        {
            return Type.GetType(typeName, throwOnError: false);
        }
        catch
        {
            return null;
        }
    }
}
