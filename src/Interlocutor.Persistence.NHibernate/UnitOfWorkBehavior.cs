namespace Interlocutor.Persistence.NHibernate;

using global::NHibernate;
using Interlocutor.Abstractions.Markers;
using Interlocutor.Abstractions.Pipeline;
using Interlocutor.Abstractions.Pipeline.Classifiers;
using Interlocutor.CommonKernel.ResultTypes;
using Interlocutor.Persistence.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

/// <summary>
/// The NHibernate commit-gate pipeline behavior: commits or rolls back whatever transaction a dispatch
/// opened, implementing the unit-of-work-owned transaction model, where <c>IUnitOfWork</c> - not a separate
/// transaction-marker-driven behavior - owns transaction begin/commit/rollback.
/// </summary>
/// <typeparam name="TRequest">
/// The request type this behavior processes. Unconstrained - unlike the retired <c>TransactionBehavior</c>,
/// no marker is required to participate. Every request gets begin-on-first-write protection once this
/// behavior is registered.
/// </typeparam>
/// <typeparam name="TResponse">The response type returned after pipeline execution.</typeparam>
/// <param name="session">Lazy factory for the NHibernate <see cref="ISession"/> instance backing the transaction.</param>
/// <param name="classifier">The outcome classifier used to determine success vs. failure for rollback decisions.</param>
/// <param name="describer">Optional custom describer for converting response outcomes to human-readable strings for logging. Defaults to a default classifier-based describer.</param>
/// <param name="logger">
/// Optional logger for a debug-level line recording the commit/rollback decision. Defaults to
/// <see cref="NullLogger{T}.Instance"/> when not supplied via DI.
/// </param>
/// <remarks>
/// <para>
/// This behavior implements only the NHibernate-specific primitives the begin/run/check/commit-or-rollback
/// algorithm needs - flush-mode priming, detecting an active transaction, detecting pending work, and
/// beginning/committing/rolling one back - behind the <see cref="NHibernateCommitGateSession"/> adapter. The
/// control flow itself lives in <see cref="CommitGateDriver"/>, shared with every other store's commit-gate
/// behavior, so this type and its EF Core counterpart can never drift on when they begin, commit, or roll back.
/// </para>
/// <para>
/// <b>Flush-mode default.</b> NHibernate's default flush mode can auto-flush pending changes before a query
/// executes, which could write to the database ahead of any transaction this behavior opens around it.
/// <see cref="NHibernateCommitGateSession"/> sets the session's <see cref="ISession.FlushMode"/> to
/// <see cref="FlushMode.Commit"/> as soon as it is constructed (unless the request opts out via
/// <see cref="IExternalStoreRequest"/>, in which case it is never constructed) so nothing can flush before a
/// transaction opens - without this, whole-session protection does not hold for NHibernate.
/// </para>
/// <para>
/// <b>Begin-on-first-write.</b> This behavior does not open a transaction unconditionally at pipeline start.
/// It opens one eagerly only when <typeparamref name="TRequest"/> implements <see cref="ITransactionalRequest"/>
/// (the narrowed "needs an eager transaction for read-then-write atomicity" meaning - see that marker's
/// remarks); otherwise the transaction begins the moment <see cref="NHibernateUnitOfWork.SaveChangesAsync"/>
/// is called mid-handler (that call begins one itself before flushing), or - for a write staged directly on
/// the session with no such call in between - is detected retroactively via <see cref="ISession.IsDirty()"/>
/// when <see cref="CommitGateDriver"/>'s own post-<c>next()</c> check runs. Either way, nothing reaches the
/// database before that begin, because the flush-mode default above blocks every flush attempt until then. At
/// pipeline completion, the driver checks <see cref="SessionExtensions.GetCurrentTransaction(ISession)"/> and
/// <see cref="ISession.IsDirty()"/> -
/// if neither indicates pending work, the dispatch never wrote anything (or an explicit
/// <see cref="Interlocutor.Persistence.Abstractions.IUnitOfWork.CancelAsync"/> already cleaned up) and nothing
/// further happens, so a pure-read dispatch never pays for an open transaction.
/// </para>
/// <para>
/// <b>Whole-session protection.</b> The commit/rollback decision is driven by the session's own
/// <c>Transaction</c>/<c>IsDirty()</c> state, not by which API staged the change - so a write through an
/// <c>IDomainRepository</c>, through <see cref="NHibernateUnitOfWork"/>, or through a directly-injected
/// <c>Lazy&lt;ISession&gt;</c> is protected uniformly, matching the retired <c>TransactionBehavior</c>'s
/// previous whole-session guarantee. Committing implicitly flushes all pending entity changes tracked by the
/// session - unlike EF Core, no explicit save call is required before committing.
/// </para>
/// <para>
/// <b>Diagnostics:</b> <see cref="CommitGateDriver"/> emits one <see cref="LogLevel.Debug"/> line per
/// invocation that actually commits or rolls back, naming which and the <paramref name="describer"/>-rendered
/// outcome that drove that decision. Nothing is logged for a dispatch that never opened a transaction.
/// </para>
/// </remarks>
public sealed class UnitOfWorkBehavior<TRequest, TResponse>(
    Lazy<ISession> session,
    IOutcomeClassifier? classifier = null,
    IResponseOutcomeDescriber? describer = null,
    ILogger<UnitOfWorkBehavior<TRequest, TResponse>>? logger = null
) : IPipelineBehavior<TRequest, TResponse>
{
    private readonly IOutcomeClassifier classifier = classifier ?? OutcomeClassifierBuilder.DefaultClassifier;

    private readonly IResponseOutcomeDescriber describer =
        describer ?? new DefaultResponseOutcomeDescriber(classifier ?? OutcomeClassifierBuilder.DefaultClassifier);

    private readonly ILogger<UnitOfWorkBehavior<TRequest, TResponse>> logger =
        logger ?? NullLogger<UnitOfWorkBehavior<TRequest, TResponse>>.Instance;

    /// <inheritdoc/>
    public Task<TResponse> HandleAsync(TRequest request, Func<Task<TResponse>> next, CancellationToken ct) =>
        CommitGateDriver.Handle(
            request,
            next,
            () => new NHibernateCommitGateSession(session.Value),
            this.classifier,
            this.describer,
            this.logger,
            ct
        );
}
