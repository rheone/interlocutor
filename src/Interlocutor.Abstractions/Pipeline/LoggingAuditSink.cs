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
/// Audit entries are formatted as structured log entries with the following properties:
/// <list type="bullet">
///   <item><description>RequestType - The type name of the audited request.</description></item>
///   <item><description>ActionName - The action being audited.</description></item>
///   <item><description>Outcome - The outcome (Success/Failure) of the operation.</description></item>
///   <item><description>CorrelationId - The correlation ID from the request context, if available.</description></item>
/// </list>
/// </para>
/// </remarks>
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
