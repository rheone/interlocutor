namespace Interlocutor.Abstractions.Pipeline;

using Interlocutor.Abstractions.Pipeline.Classifiers;

/// <summary>
/// Default <see cref="IResponseOutcomeDescriber"/>. Uses an <see cref="IOutcomeClassifier"/> to
/// produce labels. Unwraps compiler-generated unions via
/// <see cref="System.Runtime.CompilerServices.IUnion"/> before labeling.
/// </summary>
/// <remarks>
/// <para>
/// This implementation delegates labeling to an injected <see cref="IOutcomeClassifier"/>,
/// allowing outcomes to be labeled consistently across logging and auditing behaviors.
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
    /// Initializes a new instance of the <see cref="DefaultResponseOutcomeDescriber"/> class with the given outcome classifier.
    /// </summary>
    /// <param name="classifier">The classifier used to produce labels.</param>
    public DefaultResponseOutcomeDescriber(IOutcomeClassifier classifier)
    {
        this._classifier = classifier ?? throw new ArgumentNullException(nameof(classifier));
    }

    /// <inheritdoc/>
    public string Describe(object? response) => this._classifier.GetLabel(response);
}
