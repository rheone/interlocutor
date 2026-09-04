namespace TodoTasks.Api.Infrastructure;

using Interlocutor.Abstractions.Core;

/// <summary>Extension methods for <see cref="IRequestContextAccessor"/>.</summary>
public static class RequestContextAccessorExtensions
{
    /// <summary>
    /// Gets the authenticated caller's user id, or throws if the current request has none.
    /// </summary>
    /// <remarks>
    /// Every endpoint that calls this sits behind <c>RequireAuthorization()</c>, so a missing user id
    /// here is a bug, not an outcome the caller can trigger - see
    /// <c>documentation/beginners-guide-todo-api.md</c>'s "Before you start" for why this throws rather
    /// than returning a <c>Forbidden</c>/<c>Failure</c> case.
    /// </remarks>
    /// <param name="contextAccessor">The accessor to read the current request context from.</param>
    /// <returns>The authenticated caller's user id.</returns>
    /// <exception cref="InvalidOperationException">
    /// The current request context is missing, or has no <see cref="RequestContext.UserId"/>.
    /// </exception>
    public static string RequireUserId(this IRequestContextAccessor contextAccessor) =>
        contextAccessor.Context?.UserId
        ?? throw new InvalidOperationException("This endpoint requires an authenticated caller.");
}
