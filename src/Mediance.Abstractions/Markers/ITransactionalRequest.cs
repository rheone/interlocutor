namespace Mediance.Abstractions.Markers;

/// <summary>
/// Marks a request that should be executed within a database transaction.
/// </summary>
/// <remarks>
/// <para>
/// Pipeline behaviors wrap <see cref="ITransactionalRequest"/> handler execution
/// inside a transaction scope, committing changes on success or rolling back
/// on failure. Multiple transactional requests within the same scope share a
/// single transaction when supported by the underlying unit of work.
/// </para>
/// </remarks>
public interface ITransactionalRequest;
