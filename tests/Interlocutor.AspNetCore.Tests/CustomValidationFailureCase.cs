namespace Interlocutor.AspNetCore.Tests;

using Interlocutor.CommonKernel.ResultTypes;

/// <summary>
/// A hand-rolled <see cref="IValidationFailureCase"/> implementer, standing in for a consumer's own
/// validation failure type - proves <see cref="DefaultHttpOutcomeStatusMapper"/> recognizes any implementer of
/// the interface, not just the library's own concrete types.
/// </summary>
/// <param name="Messages">The validation failure messages.</param>
public readonly record struct CustomValidationFailureCase(IReadOnlyList<string> Messages) : IValidationFailureCase;
