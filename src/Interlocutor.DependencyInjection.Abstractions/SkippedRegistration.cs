namespace Interlocutor.DependencyInjection.Abstractions;

/// <summary>
/// Records a registration the builder deliberately did not make, and why - most notably the silent
/// optional-package probing behind <see cref="InterlocutorBuilder.AddValidationBehavior"/> and
/// <see cref="InterlocutorBuilder.AddTransactionBehavior"/>, which stays silent as behavior (a missing
/// optional package is not an error) but becomes visible as an entry in
/// <see cref="InterlocutorBuilder.DescribeRegistrations"/>'s report.
/// </summary>
/// <param name="Description">What was not registered - typically the type name a probe looked for.</param>
/// <param name="Reason">Why it was skipped.</param>
public readonly record struct SkippedRegistration(string Description, string Reason);
