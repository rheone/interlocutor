namespace Interlocutor.DependencyInjection.Abstractions;

/// <summary>
/// Discriminates how an <see cref="InterlocutorRegistration"/> came to be accumulated by
/// <see cref="InterlocutorBuilder"/> - which builder mechanism produced it, as opposed to
/// <see cref="RegistrationKind"/>, which describes what role it plays.
/// </summary>
/// <remarks>
/// Consumed by the registration report <see cref="InterlocutorBuilder.DescribeRegistrations"/> returns and
/// by <see cref="InterlocutorBuilder.ValidateOnBuild"/>'s conflict detection, so a consumer (or the builder
/// itself) can distinguish "this was discovered by scanning" from "this was registered explicitly" without
/// re-deriving it from call order.
/// </remarks>
public enum RegistrationProvenance
{
    /// <summary>
    /// Discovered by scanning a configured assembly (<see cref="InterlocutorBuilder.ScanAssembly"/> and its
    /// siblings) for an implementation of a mediator handler contract.
    /// </summary>
    Scanned,

    /// <summary>
    /// Registered directly by an explicit, single-target builder call - <see cref="InterlocutorBuilder.AddHandler"/>,
    /// <see cref="InterlocutorPipelineBuilder.AddBehavior"/>, a named <c>Add*Behavior</c> convenience method, a stage
    /// registration, or a default - rather than discovered by scanning or produced by a predicate match rule.
    /// </summary>
    Explicit,

    /// <summary>
    /// Produced by a predicate-based type-matching rule, which tests a rule against every candidate type
    /// found while scanning and applies its action to every match - as opposed to <see cref="Scanned"/>,
    /// which tests every candidate type against the fixed set of known handler contract interfaces.
    /// </summary>
    Matched,
}
