namespace Interlocutor.Abstractions.Pipeline;

/// <summary>Represents a single audit record written by <see cref="AuditingBehavior{TRequest,TResponse}"/>.</summary>
/// <param name="ActionName">The logical action name (e.g., <c>"User.Create"</c>, <c>"Order.Cancel"</c>) from <see cref="Markers.IAuditableRequest.ActionName"/>.</param>
/// <param name="RequestType">The request type name, used for categorizing audit entries.</param>
/// <param name="Outcome">A short outcome description (e.g., <c>"Success"</c>, <c>"Forbidden"</c>, <c>"NotFound"</c>), produced by <see cref="IResponseOutcomeDescriber"/>.</param>
/// <param name="Timestamp">The UTC timestamp when the request was processed.</param>
/// <param name="UserId">The authenticated user ID from request context, or null if anonymous.</param>
/// <remarks>
/// <para>
/// This record is created by <see cref="AuditingBehavior{TRequest, TResponse}"/> after a request implementing
/// <see cref="Markers.IAuditableRequest"/> completes. It captures enough context to answer compliance questions like
/// "who did what, when, and what was the outcome?" without exposing the full request/response data.
/// </para>
/// </remarks>
public record AuditEntry(string ActionName, string RequestType, string Outcome, DateTime Timestamp, string? UserId = null);
