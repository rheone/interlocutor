namespace Interlocutor.AspNetCore;

/// <summary>
/// A transport-agnostic description of how a failure case should be represented in an HTTP response:
/// a status code and an optional response body.
/// </summary>
/// <param name="StatusCode">The HTTP status code to use for the response.</param>
/// <param name="Body">
/// The response body, or <see langword="null"/> when the response should carry no body (e.g. a bare status code).
/// </param>
/// <remarks>
/// Produced by <see cref="IOutcomeStatusMapper"/> and consumed by both the controller-based
/// (<c>ToActionResult</c>) and minimal-API (<c>ToMinimalApiResult</c>) conversion helpers in
/// <see cref="ResultExtensions"/>, so a single mapping decision always yields the same status code and body
/// regardless of which transport surface renders it.
/// </remarks>
public sealed record OutcomeHttpMapping(int StatusCode, object? Body = null);
