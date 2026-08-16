namespace Interlocutor.DependencyInjection.Abstractions;

/// <summary>
/// A container-neutral description of a single service registration: what service type it
/// satisfies, what implementation type provides it, its lifetime, and what role it plays.
/// </summary>
/// <remarks>
/// This is the sole unit of exchange between the assembly-scanning and default-catalog logic in
/// this module and the container-specific translators in <c>Interlocutor.DependencyInjection.Microsoft</c>
/// and <c>Interlocutor.DependencyInjection.Autofac</c>. Neither container package retains its own
/// scanning loop - both translate a sequence of these records into their own registration primitives.
/// </remarks>
/// <param name="ServiceType">
/// The service (contract) type the registration satisfies. May be an open generic type definition
/// (for example, <c>IPipelineBehavior&lt;,&gt;</c>) when <see cref="Kind"/> is <see cref="RegistrationKind.Behavior"/>.
/// </param>
/// <param name="ImplementationType">
/// The concrete or open-generic type that implements <paramref name="ServiceType"/>.
/// </param>
/// <param name="Lifetime">The service lifetime the registration should use.</param>
/// <param name="Kind">The role this registration plays, used by translators and future override policy.</param>
public readonly record struct InterlocutorRegistration(
    Type ServiceType,
    Type ImplementationType,
    RegistrationLifetime Lifetime,
    RegistrationKind Kind
);
