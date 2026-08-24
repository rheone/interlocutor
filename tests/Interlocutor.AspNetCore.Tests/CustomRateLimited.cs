namespace Interlocutor.AspNetCore.Tests;

/// <summary>A domain-specific failure case, standing in for a consumer-defined custom outcome case.</summary>
/// <param name="RetryAfter">How long the caller should wait before retrying.</param>
public readonly record struct CustomRateLimited(TimeSpan RetryAfter);
