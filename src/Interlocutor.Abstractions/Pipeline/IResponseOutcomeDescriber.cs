namespace Interlocutor.Abstractions.Pipeline;

/// <summary>
/// Inspects a handler's response generically for cross-cutting concerns (logging, auditing)
/// without requiring the response to be any specific type.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="IResponseOutcomeDescriber"/> is used by pipeline behaviors to introspect responses in a type-agnostic way.
/// A response may be a compiler-generated union, a plain value, or one of the <c>Interlocutor.CommonKernel.ResultTypes</c> shapes.
/// </para>
/// <para>
/// Implement this interface if your application uses custom result types or needs domain-specific outcome labeling.
/// Register your implementation in DI to override the default <see cref="DefaultResponseOutcomeDescriber"/> pattern-matching behavior.
/// </para>
/// </remarks>
public interface IResponseOutcomeDescriber
{
    /// <summary>Produces a short, human-readable label describing the response's outcome.</summary>
    /// <remarks>
    /// Used by logging and auditing behaviors to record outcome strings like <c>"Success"</c>, <c>"NotFound"</c>,
    /// <c>"ValidationError"</c>, etc. Should be concise and suitable for logs/audit trails.
    /// </remarks>
    /// <param name="response">The response object to describe.</param>
    /// <returns>A short label describing the response outcome.</returns>
    string Describe(object? response);
}
