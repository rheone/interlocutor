namespace Mediance.Abstractions.Pipeline;

/// <summary>Represents a single audit record written by <see cref="AuditingBehavior{TRequest,TResponse}"/>.</summary>
public record AuditEntry(string ActionName, string RequestType, string Outcome, DateTime Timestamp, string? UserId = null);
