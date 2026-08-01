namespace Mediance.Abstractions.Pipeline;

/// <summary>
/// Inspects a handler's response generically for cross-cutting concerns (logging, auditing,
/// transaction commit/rollback) without requiring the response to be any specific type.
/// </summary>
/// <remarks>
/// A response may be a compiler-generated union (<c>TResponse</c> constrained to nothing in
/// particular), a plain value, or one of the <c>Mediance.CommonKernel.ResultTypes</c> shapes.
/// Register a custom implementation in the DI container to override the default pattern-matching
/// behavior for application-specific case types.
/// </remarks>
public interface IResponseOutcomeDescriber
{
    /// <summary>Produces a short, human-readable label describing the response's outcome.</summary>
    string Describe(object? response);

    /// <summary>Determines whether the response represents a failure outcome.</summary>
    bool IsFailure(object? response);
}
