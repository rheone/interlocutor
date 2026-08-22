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
/// <param name="Provenance">
/// Which builder mechanism produced this registration. Defaults to <see cref="RegistrationProvenance.Explicit"/>
/// since most construction sites (behaviors, stages, defaults, generator-emitted entries) are single-target
/// calls; <see cref="HandlerDiscovery.Discover(System.Collections.Generic.IEnumerable{System.Reflection.Assembly})"/>
/// is the one caller that overrides it to <see cref="RegistrationProvenance.Scanned"/>.
/// </param>
/// <param name="MatchedByRule">
/// When <paramref name="Provenance"/> is <see cref="RegistrationProvenance.Matched"/>, identifies which
/// <see cref="TypeMatchRule"/> produced this registration - either that rule's own
/// <see cref="TypeMatchRule.Name"/>, or its position among <see cref="InterlocutorBuilder.MatchRules"/> when
/// unnamed. <see langword="null"/> for every other <paramref name="Provenance"/>.
/// </param>
public readonly record struct InterlocutorRegistration(
    Type ServiceType,
    Type ImplementationType,
    RegistrationLifetime Lifetime,
    RegistrationKind Kind,
    RegistrationProvenance Provenance = RegistrationProvenance.Explicit,
    string? MatchedByRule = null
);
