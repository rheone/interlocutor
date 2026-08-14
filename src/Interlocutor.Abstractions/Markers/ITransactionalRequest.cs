namespace Interlocutor.Abstractions.Markers;

/// <summary>
/// Marks a request that should be executed within a database transaction.
/// </summary>
/// <remarks>
/// <para>
/// Implement this marker interface on commands/queries when you need all database changes
/// to be atomically committed or rolled back as a unit. Pipeline behaviors detect this
/// marker and wrap handler execution in a transaction using the configured ORM
/// (EF Core, NHibernate).
/// </para>
/// <para>
/// Transactional behavior is enforced by the <c>TransactionBehavior</c> implementations in
/// <c>Interlocutor.EntityFrameworkCore</c> (EF Core) or <c>Interlocutor.NHibernate</c> (NHibernate). Requests are
/// only wrapped in transactions when they implement <see cref="ITransactionalRequest"/>. Requests implementing
/// <see cref="IExternalStoreRequest"/> are explicitly excluded to prevent nesting with external transaction scopes.
/// </para>
/// </remarks>
public interface ITransactionalRequest;
