namespace Interlocutor.Abstractions.Pipeline.Classifiers;

/// <summary>
/// Labels operation outcomes for logging/auditing, by registered type, predicate, or fallback.
/// </summary>
/// <remarks>
/// <para>
/// The outcome classifier is a pure label registry: it maps a response to a human-readable label, with
/// no success/failure judgment attached. It is built using <see cref="OutcomeClassifierBuilder"/> and
/// registered as a singleton in DI so behaviors can label outcomes consistently.
/// </para>
/// <para>
/// The classifier unwraps compiler-generated unions before labeling, so it works uniformly
/// with both union and non-union response types.
/// </para>
/// </remarks>
public interface IOutcomeClassifier
{
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
