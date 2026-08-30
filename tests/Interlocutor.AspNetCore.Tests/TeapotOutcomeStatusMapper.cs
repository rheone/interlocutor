namespace Interlocutor.AspNetCore.Tests;

using Microsoft.AspNetCore.Http;

/// <summary>
/// A deliberately unusual <see cref="IOutcomeStatusMapper"/> used to prove that a consumer-registered mapper
/// is honored instead of <see cref="DefaultHttpOutcomeStatusMapper"/>, and that it can map a custom failure type.
/// </summary>
public sealed class TeapotOutcomeStatusMapper : IOutcomeStatusMapper
{
    /// <inheritdoc/>
    public OutcomeHttpMapping Map<TFailure>(TFailure failure) =>
        failure switch
        {
            CustomRateLimited rateLimited => new OutcomeHttpMapping(StatusCodes.Status429TooManyRequests, rateLimited),
            _ => new OutcomeHttpMapping(StatusCodes.Status418ImATeapot),
        };
}
