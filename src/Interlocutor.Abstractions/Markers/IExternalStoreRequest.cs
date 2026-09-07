namespace Interlocutor.Abstractions.Markers;

/// <summary>
/// Marks a request whose handler manages its own transaction scope against an external system, so the
/// store-specific commit-gate behavior should not wrap it in a local one.
/// </summary>
/// <remarks>
/// <para>
/// Implement this marker interface on commands whose handler talks to an external system (event bus, saga
/// storage, a second database, a third-party API with its own commit semantics) that already provides its own
/// durability or transactional guarantees the handler manages directly - not one this marker provides for you.
/// This is <em>not</em> a fit for the local-transactional-outbox pattern: that pattern needs an outbox-table
/// insert and the domain write to share <em>one local transaction</em> to be atomic, and this marker does the
/// opposite - see the next paragraph. A local outbox insert alongside a domain write should go through the
/// ordinary begin-on-first-write local transaction (or <see cref="ITransactionalRequest"/>, if the eager-begin
/// variant is needed) instead, with no marker on this interface at all.
/// </para>
/// <para>
/// A request implementing <see cref="IExternalStoreRequest"/> never has a local transaction opened around it
/// by the store-specific commit-gate behavior, regardless of whether it happens to touch the scoped
/// session and regardless of whether it also implements <see cref="ITransactionalRequest"/> (see that marker's
/// own remarks for the eager-begin behavior this suppresses) - this marker's meaning and mechanism did not
/// change when <c>IUnitOfWork</c> became the transaction owner rather than a pure façade. A raw
/// <c>SaveChangesAsync</c>/<c>FlushAsync</c> call inside such a handler still executes against the scoped
/// session, but with no ambient transaction the commit-gate behavior would otherwise finalize, it auto-commits
/// immediately per the store's own default behavior - there is no local atomicity to opt back into by adding
/// this marker.
/// </para>
/// </remarks>
public interface IExternalStoreRequest;
