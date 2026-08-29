namespace Interlocutor.Abstractions.Pipeline.Cases;

using Interlocutor.CommonKernel.ResultTypes;

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
/// <remarks>
/// <typeparamref name="TResponse"/> is contravariant: it appears only in the input <paramref name="response"/>
/// position, never returned, so an extractor written against a base <see cref="IOperationOutcome"/> type can
/// stand in wherever one for a more derived response type is expected. <see cref="OutcomeCaseRegistryBuilder"/>
/// still stores and looks up extractors keyed by the exact registered response type, so this is a purely
/// compile-time widening of what's assignable - it does not change which extractor runs for a given response.
/// </remarks>
public delegate bool OutcomeCaseExtractor<in TResponse>(TResponse response, out Type caseType, out object? caseValue)
    where TResponse : IOperationOutcome;
