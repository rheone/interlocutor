namespace Interlocutor.Abstractions.Markers;

/// <summary>
/// Marks a request that should be audited upon execution.
/// </summary>
/// <remarks>
/// <para>
/// Implement this marker interface on commands/queries when you want to record an audit trail of who did what and when.
/// The <see cref="Pipeline.AuditingBehavior{TRequest, TResponse}"/> pipeline behavior detects this marker and
/// records the action, outcome, and request type to the audit store after the handler completes.
/// </para>
/// <para>
/// Provide a human-readable <see cref="ActionName"/> like <c>"User.Create"</c> or <c>"Order.Cancel"</c>. Audit logs
/// typically include the user ID (from request context), timestamp, outcome (success/failure), and request type.
/// This enables compliance reporting and forensic analysis.
/// </para>
/// </remarks>
public interface IAuditableRequest
{
    /// <summary>
    /// Gets the logical name of the action recorded in the audit log.
    /// </summary>
    /// <value>
    /// A string that identifies the operation for audit purposes, such as
    /// <c>"User.Create"</c> or <c>"Order.Submit"</c>.
    /// </value>
    string ActionName { get; }
}
