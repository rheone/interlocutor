namespace Interlocutor.DependencyInjection.Abstractions;

using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Interlocutor.Abstractions.Commands;
using Interlocutor.Abstractions.Notifications;
using Interlocutor.Abstractions.Queries;

/// <summary>
/// Scans assemblies for concrete implementations of the mediator's handler contracts and expresses
/// each match as a container-neutral <see cref="InterlocutorRegistration"/>.
/// </summary>
/// <remarks>
/// This is the single copy of the handler-scanning logic previously duplicated, byte-for-byte,
/// between <c>Interlocutor.DependencyInjection.Microsoft</c> and <c>Interlocutor.DependencyInjection.Autofac</c>.
/// Both packages call <see cref="Discover(IEnumerable{Assembly})"/> and translate the result into
/// their own container's registration primitives instead of scanning independently.
/// </remarks>
public static class HandlerDiscovery
{
    /// <summary>
    /// The open generic handler contracts a concrete type is checked against.
    /// </summary>
    private static readonly Type[] HandlerContracts =
    [
        typeof(IQueryHandler<,>),
        typeof(ICommandHandler<,>),
        typeof(IVoidCommandHandler<,>),
        typeof(INotificationHandler<>),
    ];

    /// <summary>
    /// Scans the given assemblies for concrete, non-abstract classes implementing any of the
    /// mediator's handler contracts (<see cref="IQueryHandler{TQuery,TResponse}"/>,
    /// <see cref="ICommandHandler{TCommand,TResponse}"/>, <see cref="IVoidCommandHandler{TCommand,TResult}"/>,
    /// or <see cref="INotificationHandler{TNotification}"/>).
    /// </summary>
    /// <param name="assembliesToScan">The assemblies to scan for handler types, each at <see cref="RegistrationLifetime.Scoped"/>.</param>
    /// <returns>
    /// One <see cref="InterlocutorRegistration"/> per matching handler interface a type implements,
    /// each with <see cref="RegistrationKind.Handler"/>, <see cref="RegistrationLifetime.Scoped"/>, and
    /// <see cref="RegistrationProvenance.Scanned"/>. A type implementing more than one handler contract
    /// yields one registration per contract.
    /// </returns>
    [RequiresUnreferencedCode(
        "Scans every given assembly with Assembly.GetTypes() - only reached via InterlocutorBuilder.ScanAssembly* "
            + "(themselves annotated), never via generated or explicit registrations. See "
            + "documentation/trim-and-aot-support.md."
    )]
    public static IReadOnlyList<InterlocutorRegistration> Discover(IEnumerable<Assembly> assembliesToScan) =>
        Discover(assembliesToScan.Select(a => (a, RegistrationLifetime.Scoped)));

    /// <summary>
    /// Scans the given assemblies for concrete, non-abstract classes implementing any of the mediator's
    /// handler contracts, registering each assembly's matches at the lifetime paired with it - the overload
    /// <see cref="InterlocutorBuilder.ScanAssembly"/> and its siblings use once a caller specifies a lifetime
    /// per <c>Scan*</c> call rather than accepting the blanket <see cref="RegistrationLifetime.Scoped"/> default.
    /// </summary>
    /// <param name="assembliesToScan">The assemblies to scan, each paired with the lifetime to register its matches at.</param>
    /// <returns>
    /// One <see cref="InterlocutorRegistration"/> per matching handler interface a type implements, each
    /// with <see cref="RegistrationKind.Handler"/> and <see cref="RegistrationProvenance.Scanned"/>, at the
    /// lifetime its assembly was paired with.
    /// </returns>
    [RequiresUnreferencedCode(
        "Scans every given assembly with Assembly.GetTypes() - only reached via InterlocutorBuilder.ScanAssembly* "
            + "(themselves annotated), never via generated or explicit registrations. See "
            + "documentation/trim-and-aot-support.md."
    )]
    public static IReadOnlyList<InterlocutorRegistration> Discover(
        IEnumerable<(Assembly Assembly, RegistrationLifetime Lifetime)> assembliesToScan
    )
    {
        var registrations = new List<InterlocutorRegistration>();

        // Register each concrete handler against every matching Interlocutor handler interface.
        // The mediator resolves handlers by their closed generic contract at runtime, so each
        // registration must map to the exact interface (with specific type parameters) the mediator queries.
        foreach (var (assembly, lifetime) in assembliesToScan)
        {
            foreach (var type in assembly.GetTypes().Where(IsCandidateType))
            {
                registrations.AddRange(DescribeHandler(type, lifetime, RegistrationProvenance.Scanned));
            }
        }

        return registrations;
    }

    /// <summary>
    /// Whether a type is a candidate for handler-contract matching or predicate-based type matching
    /// (<see cref="InterlocutorBuilder.AddTypeMatch"/>) - a concrete, non-abstract class. The one filter
    /// applied while scanning before either checks what a type implements or tests it against a predicate.
    /// </summary>
    /// <param name="type">The type to test.</param>
    /// <returns><see langword="true"/> if <paramref name="type"/> is a concrete, non-abstract class.</returns>
    internal static bool IsCandidateType(Type type) => type is { IsClass: true, IsAbstract: false };

    /// <summary>
    /// Describes every matching handler interface a single handler implementation type implements, without
    /// scanning an assembly - the shared logic behind both <see cref="Discover(IEnumerable{Assembly})"/>'s
    /// per-type loop and <see cref="InterlocutorBuilder.AddHandler"/>'s single-type registration.
    /// </summary>
    /// <param name="handlerImplementationType">
    /// The handler implementation type to describe - concrete or an open generic type definition.
    /// </param>
    /// <param name="lifetime">The service lifetime to record on every registration produced.</param>
    /// <param name="provenance">The provenance to record on every registration produced.</param>
    /// <returns>
    /// One <see cref="InterlocutorRegistration"/> per matching handler interface <paramref name="handlerImplementationType"/>
    /// implements, or an empty list when it implements none.
    /// </returns>
    // .All, not just .Interfaces: this flows straight into an InterlocutorRegistration, whose
    // ImplementationType itself demands .All (see that record's remarks).
    internal static IReadOnlyList<InterlocutorRegistration> DescribeHandler(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type handlerImplementationType,
        RegistrationLifetime lifetime,
        RegistrationProvenance provenance
    )
    {
        var registrations = new List<InterlocutorRegistration>();

        foreach (var iface in handlerImplementationType.GetInterfaces())
        {
            if (iface.IsGenericType && HandlerContracts.Contains(iface.GetGenericTypeDefinition()))
            {
                registrations.Add(
                    new InterlocutorRegistration(
                        iface,
                        handlerImplementationType,
                        lifetime,
                        RegistrationKind.Handler,
                        provenance
                    )
                );
            }
        }

        return registrations;
    }
}
