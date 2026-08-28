namespace Interlocutor.Abstractions.Markers;

/// <summary>
/// Marks a request whose read(s) and later write(s) must be protected by one eager transaction from pipeline
/// start, rather than the default lazy, begin-on-first-write transaction every write already gets.
/// </summary>
/// <remarks>
/// <para>
/// Per <see href="../../../docs/adr/0018-unit-of-work-as-transaction-owner.md">ADR-0018</see>, this marker no
/// longer means "this request wants a transaction at all" - every request that writes, through
/// <c>IDomainRepository</c>, <c>IUnitOfWork</c>, or a directly-injected <c>Lazy&lt;TContext&gt;</c>/
/// <c>Lazy&lt;ISession&gt;</c>, gets one automatically the first time it writes, with no marker required. This
/// marker instead forces that transaction to begin eagerly, at pipeline start, instead of waiting for the
/// first write - for the check-then-act case (for example, confirming a value is still unique immediately
/// before an insert) where the ordinary begin-on-first-write default would leave a window between the
/// protective read and the write.
/// </para>
/// <para>
/// Enforced by the store-specific commit-gate behavior in <c>Interlocutor.Persistence.EntityFrameworkCore</c>
/// (EF Core) or <c>Interlocutor.Persistence.NHibernate</c> (NHibernate). Requests implementing
/// <see cref="IExternalStoreRequest"/> are excluded regardless of whether they also implement this marker -
/// see <see cref="IExternalStoreRequest"/>'s remarks.
/// </para>
/// </remarks>
public interface ITransactionalRequest;
