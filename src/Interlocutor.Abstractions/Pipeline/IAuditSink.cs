namespace Interlocutor.Abstractions.Pipeline;

/// <summary>Persists audit records produced by <see cref="AuditingBehavior{TRequest,TResponse}"/>.</summary>
/// <remarks>
/// <para>
/// Implement <see cref="IAuditSink"/> to define where and how audit entries are stored. Register your implementation
/// in DI, and the <see cref="AuditingBehavior{TRequest, TResponse}"/> will invoke it after each auditable request completes.
/// </para>
/// <para>
/// Common implementations: database table, cloud audit log, centralized logging service, or in-memory collection for testing.
/// </para>
/// </remarks>
public interface IAuditSink
{
    /// <summary>Writes an audit entry to the backing store.</summary>
    /// <param name="entry">The audit entry to record.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task RecordAsync(AuditEntry entry, CancellationToken ct);
}
