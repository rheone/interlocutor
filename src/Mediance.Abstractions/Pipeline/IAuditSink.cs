namespace Mediance.Abstractions.Pipeline;

/// <summary>Persists audit records produced by <see cref="AuditingBehavior{TRequest,TResponse}"/>.</summary>
public interface IAuditSink
{
    /// <summary>Writes an audit entry to the backing store.</summary>
    Task RecordAsync(AuditEntry entry, CancellationToken ct);
}
