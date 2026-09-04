namespace Interlocutor.Persistence.Abstractions;

using Interlocutor.Abstractions.Markers;
using Interlocutor.Abstractions.Pipeline;
using Interlocutor.Abstractions.Pipeline.Classifiers;
using Microsoft.Extensions.Logging;

/// <summary>
/// Runs the store-agnostic control flow behind every store's commit-gate <c>UnitOfWorkBehavior</c>: bypass for
/// <see cref="IExternalStoreRequest"/>, eager transaction begin for <see cref="ITransactionalRequest"/>, running
/// the next pipeline delegate, detecting whether anything is left pending, and committing or rolling back with
/// a debug-level log line naming the decision.
/// </summary>
/// <remarks>
/// <para>
/// This is the one place the begin/run/check/commit-or-rollback algorithm is written down. Each store
/// package's <c>UnitOfWorkBehavior</c> - EF Core's and NHibernate's - implements only the store-specific
/// primitives behind <see cref="ICommitGateSession"/> (how to detect an active transaction, how to detect
/// pending work, how to begin/commit/roll one back) and defers the algorithm itself to
/// <see cref="Handle{TRequest, TResponse}"/>. Keeping the algorithm here, rather than duplicated once per
/// store, keeps the two stores' commit/rollback semantics identical by construction instead of by discipline.
/// </para>
/// <para>
/// <c>sessionFactory</c> stays a factory, not an already-resolved <see cref="ICommitGateSession"/>, so a
/// request bypassed via <see cref="IExternalStoreRequest"/> never resolves the underlying store session at
/// all - preserving the guarantee that a dispatch which never touches storage never pays for session
/// construction.
/// </para>
/// </remarks>
public static class CommitGateDriver
{
    /// <summary>
    /// Runs the shared commit-gate algorithm for one dispatch.
    /// </summary>
    /// <typeparam name="TRequest">The request type this behavior processes.</typeparam>
    /// <typeparam name="TResponse">The response type returned after pipeline execution.</typeparam>
    /// <param name="request">
    /// The incoming request, inspected for the <see cref="IExternalStoreRequest"/> and
    /// <see cref="ITransactionalRequest"/> markers.
    /// </param>
    /// <param name="next">The next delegate in the pipeline chain to invoke.</param>
    /// <param name="sessionFactory">
    /// Produces the <see cref="ICommitGateSession"/> seam for the current dispatch's scoped store session.
    /// Invoked at most once, and only when <paramref name="request"/> is not an
    /// <see cref="IExternalStoreRequest"/>.
    /// </param>
    /// <param name="classifier">The outcome classifier used to determine success vs. failure for the rollback decision.</param>
    /// <param name="describer">Describer used to render the outcome for the commit/rollback debug log line.</param>
    /// <param name="logger">Logger that receives one debug-level line per invocation that actually commits or rolls back.</param>
    /// <param name="cancellationToken">A token to observe while awaiting the next delegate and the store operations below it.</param>
    /// <returns>The response returned by <paramref name="next"/>, unchanged.</returns>
    public static async Task<TResponse> Handle<TRequest, TResponse>(
        TRequest request,
        Func<Task<TResponse>> next,
        Func<ICommitGateSession> sessionFactory,
        IOutcomeClassifier classifier,
        IResponseOutcomeDescriber describer,
        ILogger logger,
        CancellationToken cancellationToken
    )
    {
        // IExternalStoreRequest opts out of local transaction management entirely, even if the handler
        // touches the scoped session - see that marker's remarks. sessionFactory is never invoked for this
        // path, so the underlying store session is never resolved either.
        if (request is IExternalStoreRequest)
        {
            return await next().ConfigureAwait(false);
        }

        var session = sessionFactory();

        // Eager begin: ITransactionalRequest's narrowed meaning is "protect my read(s) and write(s) with one
        // transaction from pipeline start," not the default lazy begin-on-first-write.
        if (request is ITransactionalRequest && !session.HasActiveTransaction)
        {
            await session.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        }

        var result = await next().ConfigureAwait(false);

        // Nothing to finalize if nothing wrote: no transaction was ever opened (pure read, or a request that
        // short-circuited before reaching a write), and nothing is staged either (or an explicit cancel
        // already rolled back and cleared the session) - same "nothing to roll back" outcome as today.
        if (!session.HasActiveTransaction && !await session.HasPendingWorkAsync(cancellationToken).ConfigureAwait(false))
        {
            return result;
        }

        if (!session.HasActiveTransaction)
        {
            await session.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        }

        if (!classifier.IsFailure(result))
        {
            await session.CommitAsync(cancellationToken).ConfigureAwait(false);
            logger.LogDebug(
                "Committed transaction for {RequestType} with outcome {Outcome}",
                typeof(TRequest).Name,
                describer.Describe(result)
            );
        }
        else
        {
            await session.RollbackAsync(cancellationToken).ConfigureAwait(false);
            logger.LogDebug(
                "Rolled back transaction for {RequestType} with outcome {Outcome}",
                typeof(TRequest).Name,
                describer.Describe(result)
            );
        }

        return result;
    }
}
