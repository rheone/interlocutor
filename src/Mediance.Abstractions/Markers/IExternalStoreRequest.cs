namespace Mediance.Abstractions.Markers;

/// <summary>
/// Marks a request whose response should be published to an external store upon successful completion.
/// </summary>
/// <remarks>
/// <para>
/// Pipeline behaviors intercept implementations of <see cref="IExternalStoreRequest"/>
/// and forward the handler result to an outbox, event bus, or other durable external
/// storage mechanism after the handler executes successfully.
/// </para>
/// </remarks>
public interface IExternalStoreRequest;
