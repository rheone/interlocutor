namespace Interlocutor.EntityFrameworkCore;

using Interlocutor.Abstractions.Markers;
using Interlocutor.Abstractions.Pipeline;
using Interlocutor.Abstractions.Pipeline.Classifiers;
using Interlocutor.CommonKernel.ResultTypes;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Wraps transactional request handler execution in an EF Core database transaction.
/// </summary>
/// <typeparam name="TRequest">
/// The request type that this behavior processes. Constrained to <see cref="ITransactionalRequest"/> so only
/// requests that opt into a transaction close this generic - a container-neutral, compile-time-checked
/// replacement for the request-shape half of the runtime check this behavior used to perform. This does not
/// replace the <see cref="IExternalStoreRequest"/> opt-out check - see the remarks on <see cref="Handle"/>.
/// </typeparam>
/// <typeparam name="TResponse">The response type returned after pipeline execution.</typeparam>
/// <typeparam name="TContext">The EF Core <see cref="DbContext"/> type providing the database connection for the transaction scope.</typeparam>
/// <param name="db">Lazy factory for the <typeparamref name="TContext"/> instance that provides database access.</param>
/// <param name="classifier">The outcome classifier used to determine success vs. failure for rollback decisions.</param>
/// <param name="describer">Optional custom describer for converting response outcomes to human-readable strings for logging. Defaults to default classifier-based describer.</param>
/// <remarks>
/// <para>
/// This behavior opens an <see cref="Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction"/> before invoking
/// the next handler in the pipeline. After the handler completes successfully, it persists pending changes
/// via <see cref="DbContext.SaveChangesAsync(CancellationToken)"/> and commits the transaction. If the handler
/// returns a classified failure result, or if <c>SaveChangesAsync</c> throws, the transaction is rolled
/// back and all accumulated changes are discarded.
/// </para>
/// <para>
/// Requests are only wrapped in a transaction when they implement <see cref="ITransactionalRequest"/> - enforced
/// at compile time by the <typeparamref name="TRequest"/> constraint, so both Microsoft DI and Autofac skip this
/// open generic registration entirely for a request type that does not implement the marker. Requests implementing
/// <see cref="IExternalStoreRequest"/> are additionally excluded via a residual runtime check in <see cref="Handle"/>
/// because they manage their own external transaction or outbox scope and should not participate in the local
/// database transaction - see that method's remarks for why this check cannot be replaced by a second generic
/// constraint.
/// </para>
/// <para>
/// This is the EF Core equivalent of <c>Interlocutor.NHibernate.TransactionBehavior{TRequest,TResponse}</c>.
/// Unlike the NHibernate version, EF Core requires an explicit <c>SaveChangesAsync</c> call to persist tracked
/// entities before the transaction can be committed.
/// </para>
/// </remarks>
public class TransactionBehavior<TRequest, TResponse, TContext>(
    Lazy<TContext> db,
    IOutcomeClassifier? classifier = null,
    IResponseOutcomeDescriber? describer = null
) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : ITransactionalRequest
    where TContext : DbContext
{
    private readonly IOutcomeClassifier classifier = classifier ?? OutcomeClassifierBuilder.DefaultClassifier;

    // Accepted for constructor-shape symmetry with LoggingBehavior/AuditingBehavior (which do consume their
    // own describer for log/audit text) - this behavior does no logging of its own, so the field is currently
    // unused. Tracked as a follow-up rather than fixed here: removing the constructor parameter is a breaking
    // API change out of scope for a warnings cleanup.
#pragma warning disable RCS1213
    private readonly IResponseOutcomeDescriber describer =
        describer ?? new DefaultResponseOutcomeDescriber(classifier ?? OutcomeClassifierBuilder.DefaultClassifier);
#pragma warning restore RCS1213

    /// <inheritdoc/>
    /// <remarks>
    /// <para>
    /// If the request implements <see cref="IExternalStoreRequest"/>, the handler is invoked without any
    /// transaction scope. This is a residual runtime check, not covered by the <typeparamref name="TRequest"/>
    /// constraint: <see cref="ITransactionalRequest"/> and <see cref="IExternalStoreRequest"/> are meant to be
    /// mutually exclusive (a request should implement exactly one - see ADR-0004), but nothing at the type
    /// level prevents a request from implementing both. A generic constraint can only require a marker's
    /// presence, not its absence, so it cannot express "and does not also implement this other interface." If
    /// a request implementing both slips through, treating <see cref="IExternalStoreRequest"/> as the
    /// deliberate override keeps this behavior from silently opening a local transaction the request's author
    /// explicitly said belongs to an external store instead.
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
        // Residual opt-out: ITransactionalRequest is already guaranteed by the TRequest constraint, so the
        // only thing left to check at runtime is the IExternalStoreRequest override - see the remarks above.
        if (request is IExternalStoreRequest)
        {
            return await next();
        }

        var context = db.Value;
        await using var transaction = await context.Database.BeginTransactionAsync(ct);

        var result = await next();

        // Use the injected classifier to determine commit vs. rollback.
        // This ensures transaction disposition is consistent with logging/auditing behavior.
        if (!this.classifier.IsFailure(result))
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
            // Outcome classified as failure (not an exception). This is an expected business outcome, not a bug,
            // so rollback any accumulated changes to keep the database consistent with the business decision.
            await transaction.RollbackAsync(ct);
        }

        return result;
    }
}
