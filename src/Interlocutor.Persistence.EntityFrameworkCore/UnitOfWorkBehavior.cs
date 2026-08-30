namespace Interlocutor.Persistence.EntityFrameworkCore;

using Interlocutor.Abstractions.Markers;
using Interlocutor.Abstractions.Pipeline;
using Interlocutor.Abstractions.Pipeline.Classifiers;
using Interlocutor.CommonKernel.ResultTypes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

/// <summary>
/// The EF Core commit-gate pipeline behavior: commits or rolls back whatever transaction a dispatch opened,
/// per <see href="../../../maintainer_docs/adr/0018-unit-of-work-as-transaction-owner.md">ADR-0018</see>'s
/// unit-of-work-owned transaction model.
/// </summary>
/// <typeparam name="TRequest">
/// The request type this behavior processes. Unconstrained - unlike the retired <c>TransactionBehavior</c>,
/// no marker is required to participate. Every request gets begin-on-first-write protection once this
/// behavior is registered.
/// </typeparam>
/// <typeparam name="TResponse">The response type returned after pipeline execution.</typeparam>
/// <typeparam name="TContext">The EF Core <see cref="DbContext"/> type providing the database connection for the transaction scope.</typeparam>
/// <param name="db">Lazy factory for the <typeparamref name="TContext"/> instance backing the transaction.</param>
/// <param name="classifier">The outcome classifier used to determine success vs. failure for rollback decisions.</param>
/// <param name="describer">Optional custom describer for converting response outcomes to human-readable strings for logging. Defaults to a default classifier-based describer.</param>
/// <param name="logger">
/// Optional logger for a debug-level line recording the commit/rollback decision. Defaults to
/// <see cref="NullLogger{T}.Instance"/> when not supplied via DI.
/// </param>
/// <remarks>
/// <para>
/// <b>Begin-on-first-write.</b> This behavior does not open a transaction unconditionally at pipeline start.
/// It opens one eagerly only when <typeparamref name="TRequest"/> implements <see cref="ITransactionalRequest"/>
/// (the narrowed "needs an eager transaction for read-then-write atomicity" meaning - see that marker's
/// remarks); otherwise the transaction begins lazily, the first time anything writes, via
/// <c>UnitOfWorkSaveChangesInterceptor</c> (registered separately on the application's <see cref="DbContext"/>)
/// or <see cref="EntityFrameworkUnitOfWork{TContext}.SaveChangesAsync"/>. At pipeline completion, this
/// behavior checks <c>Database.CurrentTransaction</c> and <c>ChangeTracker.HasChanges()</c> - if neither
/// indicates pending work, the dispatch never wrote anything (or an explicit
/// <see cref="Interlocutor.Persistence.Abstractions.IUnitOfWork.Cancel"/> already cleaned up) and this
/// behavior does nothing further, so a pure-read dispatch never pays for an open transaction.
/// </para>
/// <para>
/// <b>Whole-session protection.</b> The commit/rollback decision below is driven by the context's own
/// <c>CurrentTransaction</c>/<c>ChangeTracker</c> state, not by which API staged the change - so a write
/// through an <c>IDomainRepository</c>, through <see cref="EntityFrameworkUnitOfWork{TContext}"/>, or through
/// a directly-injected <c>Lazy&lt;TContext&gt;</c> is protected uniformly, matching the retired <c>TransactionBehavior</c>'s
/// previous whole-session guarantee.
/// </para>
/// <para>
/// <b>Diagnostics:</b> this behavior emits one <see cref="LogLevel.Debug"/> line per invocation that actually
/// commits or rolls back, naming which and the <paramref name="describer"/>-rendered outcome that drove that
/// decision. Nothing is logged for a dispatch that never opened a transaction.
/// </para>
/// </remarks>
public class UnitOfWorkBehavior<TRequest, TResponse, TContext>(
    Lazy<TContext> db,
    IOutcomeClassifier? classifier = null,
    IResponseOutcomeDescriber? describer = null,
    ILogger<UnitOfWorkBehavior<TRequest, TResponse, TContext>>? logger = null
) : IPipelineBehavior<TRequest, TResponse>
    where TContext : DbContext
{
    private readonly IOutcomeClassifier classifier = classifier ?? OutcomeClassifierBuilder.DefaultClassifier;

    private readonly IResponseOutcomeDescriber describer =
        describer ?? new DefaultResponseOutcomeDescriber(classifier ?? OutcomeClassifierBuilder.DefaultClassifier);

    private readonly ILogger<UnitOfWorkBehavior<TRequest, TResponse, TContext>> logger =
        logger ?? NullLogger<UnitOfWorkBehavior<TRequest, TResponse, TContext>>.Instance;

    /// <inheritdoc/>
    public async Task<TResponse> Handle(TRequest request, Func<Task<TResponse>> next, CancellationToken ct)
    {
        // IExternalStoreRequest opts out of local transaction management entirely, even if the handler
        // touches the scoped context - see that marker's remarks. Suppressing UnitOfWorkSaveChangesInterceptor
        // for the duration of the handler call keeps a raw SaveChangesAsync call inside it from opening a
        // local transaction this request explicitly said belongs to an external store instead.
        if (request is IExternalStoreRequest)
        {
            var wasSuppressed = UnitOfWorkSaveChangesInterceptor.IsSuppressed.Value;
            UnitOfWorkSaveChangesInterceptor.IsSuppressed.Value = true;
            try
            {
                return await next().ConfigureAwait(false);
            }
            finally
            {
                UnitOfWorkSaveChangesInterceptor.IsSuppressed.Value = wasSuppressed;
            }
        }

        var context = db.Value;

        // Eager begin: ITransactionalRequest's narrowed meaning is "protect my read(s) and write(s) with one
        // transaction from pipeline start," not the default lazy begin-on-first-write.
        if (request is ITransactionalRequest && context.Database.CurrentTransaction is null)
        {
            // Not captured/disposed here - retrieved again via Database.CurrentTransaction and disposed once,
            // below, after it's committed or rolled back.
#pragma warning disable IDISP004
            await context.Database.BeginTransactionAsync(ct).ConfigureAwait(false);
#pragma warning restore IDISP004
        }

        var result = await next().ConfigureAwait(false);

        // Nothing to finalize if nothing wrote: no transaction was ever opened (pure read, or a request that
        // short-circuited before reaching a write), and nothing is staged either (or IUnitOfWork.Cancel
        // already rolled back and cleared the tracker) - same "nothing to roll back" outcome as today.
        if (context.Database.CurrentTransaction is null && !context.ChangeTracker.HasChanges())
        {
            return result;
        }

        var transaction =
            context.Database.CurrentTransaction ?? await context.Database.BeginTransactionAsync(ct).ConfigureAwait(false);
        await using var scopedTransaction = transaction;

        if (!this.classifier.IsFailure(result))
        {
            try
            {
                // Persists whatever is still staged - a handler that already called SaveChangesAsync mid-handler
                // leaves nothing pending here, so this is a no-op in that case.
                await context.SaveChangesAsync(ct).ConfigureAwait(false);
            }
            catch
            {
                await transaction.RollbackAsync(ct).ConfigureAwait(false);
                throw;
            }

            await transaction.CommitAsync(ct).ConfigureAwait(false);
            this.logger.LogDebug(
                "Committed transaction for {RequestType} with outcome {Outcome}",
                typeof(TRequest).Name,
                this.describer.Describe(result)
            );
        }
        else
        {
            await transaction.RollbackAsync(ct).ConfigureAwait(false);
            this.logger.LogDebug(
                "Rolled back transaction for {RequestType} with outcome {Outcome}",
                typeof(TRequest).Name,
                this.describer.Describe(result)
            );
        }

        return result;
    }
}
