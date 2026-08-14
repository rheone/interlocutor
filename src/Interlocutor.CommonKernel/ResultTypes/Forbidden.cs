namespace Interlocutor.CommonKernel.ResultTypes;

/// <summary>
/// Indicates that the caller is not authorized to perform the requested operation.
/// </summary>
/// <param name="Reason">An optional description of why access was denied.</param>
/// <remarks>
/// <para>
/// Use <see cref="Forbidden"/> when a request is syntactically valid but the caller lacks permission to execute it.
/// In HTTP, this maps to a 403 status code. This is typically raised by the authorization pipeline behavior
/// when a request requires authorization and the policy check fails.
/// </para>
/// <para>
/// Provide a <paramref name="Reason"/> for logging/debugging, but avoid exposing policy details to callers for security.
/// </para>
/// </remarks>
public readonly record struct Forbidden(string? Reason = null);
