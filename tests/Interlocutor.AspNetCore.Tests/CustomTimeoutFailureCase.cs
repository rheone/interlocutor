namespace Interlocutor.AspNetCore.Tests;

using Interlocutor.CommonKernel.ResultTypes;

/// <summary>
/// A hand-rolled <see cref="ITimeoutFailureCase"/> implementer, standing in for a consumer's own
/// policy-specific timeout failure type - proves <see cref="DefaultOutcomeStatusMapper"/> recognizes any
/// implementer of the interface, not just the library's own concrete <see cref="Timeout"/>.
/// </summary>
/// <param name="Message">The human-readable description of the timeout.</param>
public readonly record struct CustomTimeoutFailureCase(string? Message) : ITimeoutFailureCase;
