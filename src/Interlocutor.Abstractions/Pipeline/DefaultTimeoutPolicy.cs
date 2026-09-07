namespace Interlocutor.Abstractions.Pipeline;

using Interlocutor.Abstractions.Markers;

/// <summary>
/// Default <see cref="ITimeoutPolicy"/> implementation that leaves each request's declared budget unchanged.
/// </summary>
/// <remarks>
/// This implementation simply returns <see cref="ITimeoutRequest.Budget"/> as-is - suitable whenever requests
/// are trusted to declare their own correct budget and no centralized override (a tenant-wide ceiling, an
/// environment-specific multiplier, etc.) is needed. Replace it with a custom <see cref="ITimeoutPolicy"/>
/// (or a <see cref="DelegateTimeoutPolicy"/>) when that trust doesn't hold.
/// </remarks>
public sealed class DefaultTimeoutPolicy : ITimeoutPolicy
{
    /// <summary>Gets the singleton instance.</summary>
    public static readonly DefaultTimeoutPolicy Instance = new();

    /// <inheritdoc/>
    public TimeSpan ResolveBudget(ITimeoutRequest request) => request.Budget;
}
