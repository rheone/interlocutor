namespace Interlocutor.Abstractions.Pipeline;

using Microsoft.Extensions.Logging;

/// <summary>
/// Default <see cref="IAuditSink"/> implementation that records audit entries through the logging abstraction.
/// </summary>
/// <remarks>
/// <para>
/// Uses <see cref="ILogger"/> to record audit entries, with each entry logged at Information level. Suitable for development
/// and testing environments where audit data is captured via standard application logging.
/// </para>
/// <para>
/// Each entry is formatted as a structured log message carrying three of <see cref="AuditEntry"/>'s
/// properties as named placeholders:
/// <list type="bullet">
///   <item><description>RequestType - the type name of the audited request.</description></item>
///   <item><description>ActionName - the action being audited.</description></item>
///   <item><description>Outcome - the outcome label produced for the operation, e.g. <c>"Success"</c> or <c>"NotFound"</c>.</description></item>
/// </list>
/// <see cref="AuditEntry.Timestamp"/> and <see cref="AuditEntry.UserId"/> are not included in the log
/// message - the ambient log entry's own timestamp substitutes for the former, but nothing here surfaces
/// the latter. An application that needs to query audit history by user or timestamp should register a
/// dedicated <see cref="IAuditSink"/> that persists <see cref="AuditEntry"/> in full instead of relying on
/// this development-oriented implementation.
/// </para>
/// </remarks>
/// <param name="logger">The logger audit entries are written to, at Information level.</param>
public sealed class LoggingAuditSink(ILogger<LoggingAuditSink> logger) : IAuditSink
{
    private readonly ILogger<LoggingAuditSink> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    /// <inheritdoc/>
    public Task RecordAsync(AuditEntry entry, CancellationToken ct)
    {
        this._logger.LogInformation(
            "Audit: {RequestType} - {ActionName} completed with outcome {Outcome}",
            entry.RequestType,
            entry.ActionName,
            entry.Outcome
        );

        return Task.CompletedTask;
    }
}
