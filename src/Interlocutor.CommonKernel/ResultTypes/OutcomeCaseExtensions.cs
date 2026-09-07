namespace Interlocutor.CommonKernel.ResultTypes;

using System;

/// <summary>
/// Convenience projections over an arbitrary response, checking whether it implements
/// <see cref="IOutcomeCase"/> and returning one of its members, or <see langword="null"/> if it doesn't.
/// </summary>
/// <remarks>
/// Every reflection-based consumer that needs to fall back to <see cref="IOutcomeCase"/> (the outcome
/// classifier, the outcome case registry, AspNetCore's HTTP mapping) was independently repeating the same
/// <c>response is IOutcomeCase ...</c> pattern match - this centralizes it in one place, so a future change
/// to how that fallback is resolved only needs to land here.
/// </remarks>
public static class OutcomeCaseExtensions
{
    extension(object? response)
    {
        /// <summary>
        /// Gets <see cref="IOutcomeCase.ActiveCaseType"/> if <paramref name="response"/> implements
        /// <see cref="IOutcomeCase"/> and has a non-<see langword="null"/> active case type, or
        /// <see langword="null"/> otherwise.
        /// </summary>
        /// <returns><see cref="IOutcomeCase.ActiveCaseType"/>, or <see langword="null"/>.</returns>
        public Type? AsOutcomeCaseActiveType() =>
            response is IOutcomeCase { ActiveCaseType: { } activeCaseType } ? activeCaseType : null;
    }
}
