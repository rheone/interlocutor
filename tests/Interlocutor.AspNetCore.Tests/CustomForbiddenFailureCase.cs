namespace Interlocutor.AspNetCore.Tests;

using Interlocutor.CommonKernel.ResultTypes;

/// <summary>
/// A hand-rolled <see cref="IForbiddenFailureCase"/> implementer, standing in for a consumer's own
/// authorization failure type - proves <see cref="DefaultOutcomeStatusMapper"/> recognizes any implementer of
/// the interface, not just the library's own concrete <see cref="Forbidden"/>.
/// </summary>
/// <param name="Reason">The description of why access was denied.</param>
public readonly record struct CustomForbiddenFailureCase(string? Reason) : IForbiddenFailureCase;
