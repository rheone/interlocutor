namespace Mediance.EntityFrameworkCore;

using Mediance.Abstractions.Markers;
using Mediance.Abstractions.Pipeline;
using Mediance.Abstractions.Pipeline.Classifiers;
using Mediance.CommonKernel.ResultTypes;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Wraps transactional request handler execution in an EF Core database transaction.
/// </summary>
/// <typeparam name="TRequest">The request type that this behavior processes.</typeparam>
/// <typeparam name="TResponse">The response type returned after pipeline execution.</typeparam>
/// <typeparam name="TContext">The EF Core <see cref="DbContext"/> type providing the database connection for the transaction scope.</typeparam>
/// <param name="db">Lazy factory for the <typeparamref name="TContext"/> instance that provides database access.</param>
/// <param name="describer">Optional custom describer for converting response outcomes to human-readable strings for logging. Defaults to default classifier-based describer.</param>
/// <remarks>
/// <para>
/// This behavior opens an <see cref="Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction"/> before invoking
/// the next handler in the pipeline. After the handler completes successfully, it persists pending changes
/// via <see cref="DbContext.SaveChangesAsync(CancellationToken)"/> and commits the transaction. If the handler
/// returns a <see cref="Failure"/> result, or if <c>SaveChangesAsync</c> throws, the transaction is rolled
/// back and all accumulated changes are discarded.
/// </para>
/// <para>
/// Requests are only wrapped in a transaction when they implement <see cref="ITransactionalRequest"/>.
/// Requests implementing <see cref="IExternalStoreRequest"/> are explicitly excluded because they manage
/// their own external transaction or outbox scope and should not participate in the local database transaction.
/// </para>
/// <para>
/// This is the EF Core equivalent of <c>Mediance.NHibernate.TransactionBehavior{TRequest,TResponse}</c>.
/// Unlike the NHibernate version, EF Core requires an explicit <c>SaveChangesAsync</c> call to persist tracked
/// entities before the transaction can be committed.
/// </para>
/// </remarks>
public class TransactionBehavior<TRequest, TResponse, TContext>(Lazy<TContext> db, IResponseOutcomeDescriber? describer = null)
    : IPipelineBehavior<TRequest, TResponse>
    where TContext : DbContext
{
    private readonly IResponseOutcomeDescriber describer = describer ?? new DefaultResponseOutcomeDescriber(OutcomeClassifierBuilder.DefaultClassifier);

    /// <inheritdoc/>
    /// <remarks>
    /// <para>
    /// If the request does not implement <see cref="ITransactionalRequest"/> or implements
    /// <see cref="IExternalStoreRequest"/>, the handler is invoked without any transaction scope.
    /// </para>
    /// <para>
    /// When a transaction is active, <see cref="DbContext.SaveChangesAsync(CancellationToken)"/> is called
    /// after the handler succeeds to persist all tracked entity changes within the transaction. If
    /// <c>SaveChangesAsync</c> throws, the transaction is rolled back before the exception re-propagates.
    /// If the handler returns a <see cref="Failure"/> result, the transaction is rolled back without
    /// attempting to persist changes.
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

        var context = db.Value;
        await using var transaction = await context.Database.BeginTransactionAsync(ct);

        var result = await next();

        if (!this.describer.IsFailure(result))
        {
            try
            {
                // Persist all tracked entity changes accumulated during handler execution. In EF Core,
                // entities are tracked by the DbContext but not written to the database until SaveChangesAsync
                // is called, which must happen while the transaction is still open.
                await context.SaveChangesAsync(ct);
            }
            catch
            {
                // SaveChangesAsync threw, meaning the database operation failed or validation failed.
                // Rollback discards any partial changes and closes the transaction in a failed state.
                await transaction.RollbackAsync(ct);
                throw;
            }

            // All changes persisted successfully. Committing the transaction makes all the SaveChangesAsync
            // changes durable and atomic.
            await transaction.CommitAsync(ct);
        }
        else
        {
            // Handler returned a failure result (not an exception). This is an expected business outcome, not a bug,
            // so rollback any accumulated changes to keep the database consistent with the business decision.
            await transaction.RollbackAsync(ct);
        }

        return result;
    }
}
