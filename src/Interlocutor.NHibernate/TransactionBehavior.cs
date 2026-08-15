namespace Interlocutor.NHibernate;

using global::NHibernate;
using Interlocutor.Abstractions.Markers;
using Interlocutor.Abstractions.Pipeline;
using Interlocutor.Abstractions.Pipeline.Classifiers;
using Interlocutor.CommonKernel.ResultTypes;

/// <summary>
/// Wraps transactional request handler execution in an NHibernate database transaction.
/// </summary>
/// <typeparam name="TRequest">
/// The request type that this behavior processes. Constrained to <see cref="ITransactionalRequest"/> so only
/// requests that opt into a transaction close this generic — a container-neutral, compile-time-checked
/// replacement for the request-shape half of the runtime check this behavior used to perform. This does not
/// replace the <see cref="IExternalStoreRequest"/> opt-out check — see the remarks on <see cref="Handle"/>.
/// </typeparam>
/// <typeparam name="TResponse">The response type returned after pipeline execution.</typeparam>
/// <param name="session">Lazy factory for the NHibernate <see cref="ISession"/> instance that provides the database transaction.</param>
/// <param name="classifier">The outcome classifier used to determine success vs. failure for rollback decisions.</param>
/// <param name="describer">Optional custom describer for converting response outcomes to human-readable strings for logging. Defaults to default classifier-based describer.</param>
/// <remarks>
/// <para>
/// This behavior begins an <see cref="ITransaction"/> on the NHibernate <see cref="ISession"/> before invoking
/// the next handler in the pipeline. If the handler completes successfully, the transaction is committed,
/// which implicitly flushes all pending entity changes tracked by the session. If the handler returns a
/// classified failure result, the transaction is rolled back and any unflushed changes are discarded.
/// </para>
/// <para>
/// Requests are only wrapped in a transaction when they implement <see cref="ITransactionalRequest"/> — enforced
/// at compile time by the <typeparamref name="TRequest"/> constraint, so both Microsoft DI and Autofac skip this
/// open generic registration entirely for a request type that does not implement the marker. Requests implementing
/// <see cref="IExternalStoreRequest"/> are additionally excluded via a residual runtime check in <see cref="Handle"/>
/// because they manage their own external transaction or outbox scope and should not participate in the local
/// database transaction — see that method's remarks for why this check cannot be replaced by a second generic
/// constraint.
/// </para>
/// <para>
/// This is the NHibernate equivalent of <c>Interlocutor.EntityFrameworkCore.TransactionBehavior{TRequest,TResponse,TContext}</c>.
/// Unlike the EF Core version, NHibernate's <see cref="ISession"/> automatically tracks pending changes, so
/// no explicit save or flush call is required before committing the transaction.
/// </para>
/// </remarks>
public class TransactionBehavior<TRequest, TResponse>(
    Lazy<ISession> session,
    IOutcomeClassifier? classifier = null,
    IResponseOutcomeDescriber? describer = null
) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : ITransactionalRequest
{
    private readonly IOutcomeClassifier classifier = classifier ?? OutcomeClassifierBuilder.DefaultClassifier;
    private readonly IResponseOutcomeDescriber _describer =
        describer ?? new DefaultResponseOutcomeDescriber(classifier ?? OutcomeClassifierBuilder.DefaultClassifier);

    /// <inheritdoc/>
    /// <remarks>
    /// <para>
    /// If the request implements <see cref="IExternalStoreRequest"/>, the handler is invoked without any
    /// transaction scope. This is a residual runtime check, not covered by the <typeparamref name="TRequest"/>
    /// constraint: <see cref="ITransactionalRequest"/> and <see cref="IExternalStoreRequest"/> are meant to be
    /// mutually exclusive (a request should implement exactly one — see ADR-0004), but nothing at the type
    /// level prevents a request from implementing both. A generic constraint can only require a marker's
    /// presence, not its absence, so it cannot express "and does not also implement this other interface." If
    /// a request implementing both slips through, treating <see cref="IExternalStoreRequest"/> as the
    /// deliberate override keeps this behavior from silently opening a local transaction the request's author
    /// explicitly said belongs to an external store instead.
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
        // Residual opt-out: ITransactionalRequest is already guaranteed by the TRequest constraint, so the
        // only thing left to check at runtime is the IExternalStoreRequest override — see the remarks above.
        if (request is IExternalStoreRequest)
        {
            return await next();
        }

        var s = session.Value;
        using var transaction = s.BeginTransaction();

        var result = await next();

        // Use the injected classifier to determine commit vs. rollback.
        // This ensures transaction disposition is consistent with logging/auditing behavior.
        if (!this.classifier.IsFailure(result))
        {
            // Handler succeeded. CommitAsync flushes all pending entity changes tracked by the session to the database
            // and then commits the transaction, making all changes durable and atomic. Unlike EF Core, NHibernate
            // doesn't require an explicit SaveChanges call — flushing happens automatically on commit.
            await transaction.CommitAsync(ct);
        }
        else
        {
            // Outcome classified as failure (not an exception). This is an expected business outcome, not a bug,
            // so rollback any unflushed changes to keep the database consistent with the business decision.
            await transaction.RollbackAsync(ct);
        }

        return result;
    }
}
