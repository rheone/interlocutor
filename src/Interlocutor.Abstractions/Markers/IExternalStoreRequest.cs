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
/// A request implementing <see cref="IExternalStoreRequest"/> never has a local transaction opened around it
/// by the store-specific commit-gate behavior, regardless of whether it happens to touch the scoped
/// session and regardless of whether it also implements <see cref="ITransactionalRequest"/> - this marker's
/// meaning and mechanism are unchanged by
/// <see href="../../../maintainer_docs/adr/0018-unit-of-work-as-transaction-owner.md">ADR-0018</see>. Use this when your
/// handler manages its own external transaction scope.
/// </para>
/// </remarks>
public interface IExternalStoreRequest;
