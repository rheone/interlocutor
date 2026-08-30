namespace Interlocutor.AspNetCore.Tests;

using Interlocutor.CommonKernel.ResultTypes;

/// <summary>
/// A hand-rolled <see cref="IRateLimitedFailureCase"/> implementer, standing in for a consumer's own
/// limiter-specific failure type - proves <see cref="DefaultHttpOutcomeStatusMapper"/> recognizes any implementer
/// of the interface, not just the library's own concrete <see cref="RateLimited"/>. Distinct from
/// <see cref="CustomRateLimited"/>, which deliberately implements no marker interface at all.
/// </summary>
/// <param name="Message">The human-readable description of the rate limit that was exceeded.</param>
public readonly record struct CustomRateLimitedFailureCase(string? Message) : IRateLimitedFailureCase;
