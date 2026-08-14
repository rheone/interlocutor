namespace Interlocutor.Abstractions.Pipeline;

using Interlocutor.Abstractions.Pipeline.Classifiers;

/// <summary>
/// Default <see cref="IResponseOutcomeDescriber"/>. Uses an <see cref="IOutcomeClassifier"/> to
/// determine failure/success classification and labels. Unwraps compiler-generated unions via
/// <see cref="IUnion"/> before classification.
/// </summary>
/// <remarks>
/// <para>
/// This implementation delegates classification to an injected <see cref="IOutcomeClassifier"/>,
/// allowing outcomes to be classified consistently across logging, auditing, and transaction behaviors.
/// The classifier is built by <see cref="OutcomeClassifierBuilder"/> and includes all kernel types
/// plus any consumer-registered custom types and predicates.
/// </para>
/// <para>
/// Register a custom <see cref="IResponseOutcomeDescriber"/> if you need outcome labeling
/// behavior different from the classifier's (e.g., domain-specific formatting).
/// </para>
/// </remarks>
public sealed class DefaultResponseOutcomeDescriber : IResponseOutcomeDescriber
{
    private readonly IOutcomeClassifier _classifier;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultResponseOutcomeDescriber"/> class.
    /// Initializes a new instance with the given outcome classifier.
    /// </summary>
    /// <param name="classifier">The classifier used to determine success/failure and labels.</param>
    public DefaultResponseOutcomeDescriber(IOutcomeClassifier classifier)
    {
        this._classifier = classifier ?? throw new ArgumentNullException(nameof(classifier));
    }

    /// <inheritdoc/>
    public string Describe(object? response) => this._classifier.GetLabel(response);

    /// <inheritdoc/>
    public bool IsFailure(object? response) => this._classifier.IsFailure(response);
}
