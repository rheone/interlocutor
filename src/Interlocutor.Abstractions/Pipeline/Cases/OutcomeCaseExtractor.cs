namespace Interlocutor.Abstractions.Pipeline.Cases;

using Interlocutor.CommonKernel;

/// <summary>
/// Extracts the active case type/value from a <typeparamref name="TResponse"/> for
/// <see cref="OutcomeCaseRegistryBuilder.RegisterExtractor{TResponse}(OutcomeCaseExtractor{TResponse})"/>.
/// </summary>
/// <typeparam name="TResponse">The <see cref="IOperationOutcome"/> type to extract from.</typeparam>
/// <param name="response">The response to extract the active case from. Never <see langword="null"/> - the
/// registry built by <see cref="OutcomeCaseRegistryBuilder"/> handles <see langword="null"/> itself before
/// delegating to a registered extractor.</param>
/// <param name="caseType">The runtime type of the active case.</param>
/// <param name="caseValue">The active case value.</param>
/// <returns><see langword="true"/> if the active case was extracted; otherwise, <see langword="false"/>.</returns>
public delegate bool OutcomeCaseExtractor<TResponse>(TResponse response, out Type caseType, out object? caseValue)
    where TResponse : IOperationOutcome;
