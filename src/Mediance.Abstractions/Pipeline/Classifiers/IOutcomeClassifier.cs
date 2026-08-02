namespace Mediance.Abstractions.Pipeline.Classifiers;

/// <summary>
/// Classifies operation outcomes as success or failure, determining transaction disposition
/// and logging/auditing behavior.
/// </summary>
/// <remarks>
/// <para>
/// The outcome classifier encapsulates the logic that determines whether a response represents
/// a domain failure (rollback) or success (commit). It is built using <see cref="OutcomeClassifierBuilder"/>
/// and registered as a singleton in DI so behaviors can classify outcomes consistently.
/// </para>
/// <para>
/// The classifier unwraps compiler-generated unions before classification, so it works uniformly
/// with both union and non-union response types.
/// </para>
/// </remarks>
public interface IOutcomeClassifier
{
    /// <summary>
    /// Determines whether the given response represents a failure outcome.
    /// </summary>
    /// <remarks>
    /// Returns true if the response is registered as a failure type, matches a failure predicate,
    /// or follows the unregistered-type policy (default: fail-closed, unknown = failure).
    /// </remarks>
    /// <param name="response">The response to classify. May be null.</param>
    /// <returns><see langword="true"/> if the response represents a failure; otherwise, <see langword="false"/>.</returns>
    bool IsFailure(object? response);

    /// <summary>
    /// Gets a human-readable label for the outcome.
    /// </summary>
    /// <remarks>
    /// Used by logging and auditing to record outcome strings. Returns type-specific labels for
    /// registered types (e.g., "NotFound", "ValidationError"), the registered custom label for
    /// consumer types, or the runtime type name as fallback.
    /// </remarks>
    /// <param name="response">The response to label. May be null.</param>
    /// <returns>A short label describing the response outcome.</returns>
    string GetLabel(object? response);
}
