namespace Mediance.NHibernate;

using global::NHibernate;
using Mediance.Abstractions.Markers;
using Mediance.Abstractions.Pipeline;
using Mediance.Abstractions.Pipeline.Classifiers;
using Mediance.CommonKernel.ResultTypes;

/// <summary>
/// Wraps transactional request handler execution in an NHibernate database transaction.
/// </summary>
/// <typeparam name="TRequest">The request type that this behavior processes.</typeparam>
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
public class TransactionBehavior<TRequest, TResponse>(
    Lazy<ISession> session,
    IOutcomeClassifier? classifier = null,
    IResponseOutcomeDescriber? describer = null
) : IPipelineBehavior<TRequest, TResponse>
{
    private readonly IOutcomeClassifier classifier = classifier ?? OutcomeClassifierBuilder.DefaultClassifier;
    private readonly IResponseOutcomeDescriber _describer =
        describer ?? new DefaultResponseOutcomeDescriber(classifier ?? OutcomeClassifierBuilder.DefaultClassifier);

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
        if (request is not ITransactionalRequest or IExternalStoreRequest)
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
