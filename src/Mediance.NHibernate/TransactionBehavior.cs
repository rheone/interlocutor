using Mediance.Abstractions;
using Mediance.Abstractions.Markers;
using Mediance.Abstractions.Pipeline;
using Mediance.CommonKernel.ResultTypes;
using NHibernate;

namespace Mediance.NHibernate;

/// <summary>
/// Wraps transactional request handler execution in an NHibernate database transaction.
/// </summary>
/// <typeparam name="TRequest">The request type that this behavior processes.</typeparam>
/// <typeparam name="TResponse">The response type returned after pipeline execution.</typeparam>
/// <param name="session">Lazy factory for the NHibernate <see cref="ISession"/> instance that provides the database transaction.</param>
/// <param name="describer">Optional custom describer for converting response outcomes to human-readable strings for logging. Defaults to <see cref="DefaultResponseOutcomeDescriber.Instance"/>.</param>
/// <remarks>
/// <para>
/// This behavior begins an <see cref="ITransaction"/> on the NHibernate <see cref="ISession"/> before invoking
/// the next handler in the pipeline. If the handler completes successfully, the transaction is committed,
/// which implicitly flushes all pending entity changes tracked by the session. If the handler returns a
/// <see cref="Failure"/> result, the transaction is rolled back and any unflushed changes are discarded.
/// </para>
/// <para>
/// Requests are only wrapped in a transaction when they implement <see cref="ITransactionalRequest"/>.
/// Requests implementing <see cref="IExternalStoreRequest"/> are explicitly excluded because they manage
/// their own external transaction or outbox scope and should not participate in the local database transaction.
/// </para>
/// <para>
/// This is the NHibernate equivalent of <c>Mediance.EntityFrameworkCore.TransactionBehavior{TRequest,TResponse,TContext}</c>.
/// Unlike the EF Core version, NHibernate's <see cref="ISession"/> automatically tracks pending changes, so
/// no explicit save or flush call is required before committing the transaction.
/// </para>
/// </remarks>
public class TransactionBehavior<TRequest, TResponse>(Lazy<ISession> session, IResponseOutcomeDescriber? describer = null)
    : IPipelineBehavior<TRequest, TResponse>
{
    private readonly IResponseOutcomeDescriber describer = describer ?? DefaultResponseOutcomeDescriber.Instance;

    /// <inheritdoc/>
    /// <remarks>
    /// <para>
    /// If the request does not implement <see cref="ITransactionalRequest"/> or implements
    /// <see cref="IExternalStoreRequest"/>, the handler is invoked without any transaction scope.
    /// </para>
    /// <para>
    /// When a transaction is active, the handler is executed within an NHibernate <see cref="ITransaction"/>.
    /// Committing the transaction implicitly flushes all pending entity changes tracked by the session.
    /// If the handler returns a <see cref="Failure"/> result, the transaction is rolled back and any
    /// unflushed changes are discarded.
    /// </para>
    /// </remarks>
    public async Task<TResponse> Handle(TRequest request, Func<Task<TResponse>> next, CancellationToken ct)
    {
        // Skip transaction management for requests that do not participate in transactions
        // or manage their own external transaction context (e.g., outbox, event bus).
        if (request is not ITransactionalRequest || request is IExternalStoreRequest)
        {
            return await next();
        }

        var s = session.Value;
        using var transaction = s.BeginTransaction();

        var result = await next();

        if (!this.describer.IsFailure(result))
        {
            // Handler succeeded — commit flushes pending changes and completes the transaction.
            await transaction.CommitAsync(ct);
        }
        else
        {
            // Handler returned a failure result — discard any unflushed changes.
            await transaction.RollbackAsync(ct);
        }

        return result;
    }
}
