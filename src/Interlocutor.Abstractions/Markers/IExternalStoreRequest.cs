namespace Interlocutor.Abstractions.Markers;

/// <summary>
/// Marks a request whose response should be published to an external store upon successful completion.
/// </summary>
/// <remarks>
/// <para>
/// Implement this marker interface on commands when the handler's response should be durably persisted
/// to an external system (outbox, event bus, saga storage) after successful execution. This pattern
/// ensures reliable event delivery even if the message broker or external system is temporarily unavailable.
/// </para>
/// <para>
/// Requests implementing <see cref="IExternalStoreRequest"/> are explicitly excluded from local database
/// transactions (via <see cref="ITransactionalRequest"/>) to prevent nesting or deadlocks. Use this when
/// your handler manages its own external transaction scope.
/// </para>
/// </remarks>
public interface IExternalStoreRequest;
