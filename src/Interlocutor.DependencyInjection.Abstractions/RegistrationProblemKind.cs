namespace Interlocutor.DependencyInjection.Abstractions;

/// <summary>
/// Discriminates what kind of issue a <see cref="RegistrationProblem"/> reports - every check
/// <see cref="InterlocutorBuilder.ValidateOnBuild"/> performs against the accumulated registration list.
/// </summary>
public enum RegistrationProblemKind
{
    /// <summary>
    /// A request type discovered while scanning has no matching <see cref="RegistrationKind.Handler"/> registration.
    /// </summary>
    UnhandledRequestType,

    /// <summary>
    /// A match rule marked as requiring at least one match (see the type-matching registration builder
    /// methods) matched no candidate type.
    /// </summary>
    UnmatchedRequiredRule,

    /// <summary>
    /// Two registrations of the same (service type, implementation type) pair were accumulated with
    /// different <see cref="RegistrationLifetime"/> values.
    /// </summary>
    ConflictingLifetime,
}
