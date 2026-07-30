namespace Mediance.Abstractions.Markers;

/// <summary>
/// Marks a request that should be audited upon execution.
/// </summary>
/// <remarks>
/// <para>
/// Pipeline behaviors intercept implementations of <see cref="IAuditableRequest"/>
/// and record the <see cref="ActionName"/> along with request metadata to the
/// audit store before the request handler executes.
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
