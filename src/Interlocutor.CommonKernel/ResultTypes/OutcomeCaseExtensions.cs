namespace Interlocutor.CommonKernel.ResultTypes;

using System;

/// <summary>
/// Convenience projections over an arbitrary response, checking whether it implements
/// <see cref="IOutcomeCase"/> and returning one of its members, or <see langword="null"/> if it doesn't.
/// </summary>
/// <remarks>
/// The outcome classifier and the outcome case registry each independently repeated the same
/// <c>response is IOutcomeCase ...</c> pattern match to fall back to <see cref="IOutcomeCase"/> - this
/// centralizes it in one place, so a future change to how that fallback is resolved only needs to land here.
/// AspNetCore's HTTP mapping never repeated the pattern itself, but benefits the same way: it resolves every
/// response's active case through the outcome case registry, which uses this member internally.
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
