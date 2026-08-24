namespace Interlocutor.DependencyInjection.Abstractions;

/// <summary>
/// A single problem <see cref="InterlocutorBuilder.ValidateOnBuild"/> found while checking the
/// accumulated registration list - answerable from that list alone, with no live container needed.
/// </summary>
/// <param name="Kind">What kind of problem this is.</param>
/// <param name="Description">A human-readable description of the specific problem found.</param>
public readonly record struct RegistrationProblem(RegistrationProblemKind Kind, string Description);
