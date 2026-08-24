namespace Interlocutor.DependencyInjection.Abstractions;

/// <summary>
/// A predicate-based registration rule added via <see cref="InterlocutorBuilder.AddTypeMatch"/> - tested
/// against every candidate type found while scanning, with every match registered as
/// <see cref="Target"/> describes, minus anything <see cref="Except"/> carves out.
/// </summary>
/// <remarks>
/// This is the reverse cardinality of the existing marker-interface-based behavior scoping and of
/// <c>OutcomeClassifierBuilder.RegisterPredicate</c> (which tests one runtime value against many
/// predicates, first match wins): here, one rule is tested against many candidate types, and every type
/// that matches gets the rule's action applied - not just the first.
/// </remarks>
/// <param name="Predicate">Tested against every candidate type found while scanning; every match is registered.</param>
/// <param name="Target">What role each matching type is registered as.</param>
/// <param name="Lifetime">The service lifetime to register each match with.</param>
/// <param name="Except">
/// An optional carve-out predicate - a candidate type matching <see cref="Predicate"/> is skipped if this
/// also returns <see langword="true"/> for it. <see langword="null"/> excludes nothing.
/// </param>
/// <param name="Require">
/// Whether this rule is required to match at least one candidate type. Recorded here for
/// <see cref="InterlocutorBuilder.ValidateOnBuild"/> to check; this record does not enforce it itself.
/// </param>
/// <param name="Name">
/// An optional label identifying this rule, carried onto every <see cref="InterlocutorRegistration"/> it
/// produces (<see cref="InterlocutorRegistration.MatchedByRule"/>) so <see cref="RegistrationReport.Format"/>
/// can distinguish which rule matched a given type when more than one is configured. Defaults to this
/// rule's position among <see cref="InterlocutorBuilder.MatchRules"/> when not given.
/// </param>
public sealed record TypeMatchRule(
    Func<Type, bool> Predicate,
    TypeMatchTarget Target,
    RegistrationLifetime Lifetime,
    Func<Type, bool>? Except,
    bool Require,
    string? Name = null
);
