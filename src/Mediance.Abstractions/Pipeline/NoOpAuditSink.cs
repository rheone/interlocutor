namespace Mediance.Abstractions.Pipeline;

/// <summary>
/// No-operation <see cref="IAuditSink"/> implementation that discards all audit entries.
/// </summary>
/// <remarks>
/// <para>
/// Use this implementation to disable audit recording without removing <see cref="AuditingBehavior{TRequest,TResponse}"/> from the pipeline.
/// Useful for test environments or when audit functionality is temporarily disabled.
/// </para>
/// </remarks>
public sealed class NoOpAuditSink : IAuditSink
{
    /// <summary>Gets the singleton instance.</summary>
    public static readonly NoOpAuditSink Instance = new();

    /// <inheritdoc/>
    public Task RecordAsync(AuditEntry entry, CancellationToken ct) => Task.CompletedTask;
}
