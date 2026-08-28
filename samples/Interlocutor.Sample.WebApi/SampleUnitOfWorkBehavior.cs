namespace Interlocutor.Sample.WebApi;

using Interlocutor.Abstractions.Pipeline;
using Interlocutor.Abstractions.Pipeline.Classifiers;
using Interlocutor.Persistence.EntityFrameworkCore;
using Interlocutor.Sample.Handlers;

/// <summary>
/// Closes <see cref="UnitOfWorkBehavior{TRequest,TResponse,TContext}"/>'s <c>TContext</c> parameter over
/// <see cref="SampleDbContext"/>, leaving <c>TRequest</c>/<c>TResponse</c> open.
/// </summary>
/// <remarks>
/// Microsoft.Extensions.DependencyInjection requires an open generic service registration's implementation
/// type to have the <em>same</em> number of open type parameters as the service interface it's registered
/// against - it cannot leave a third type parameter (<c>TContext</c>) open behind a two-parameter
/// <c>IPipelineBehavior&lt;TRequest,TResponse&gt;</c>. Registering
/// <see cref="UnitOfWorkBehavior{TRequest,TResponse,TContext}"/> directly (as
/// <c>InterlocutorPipelineBuilder.AddUnitOfWorkBehavior()</c> does) therefore throws at container build
/// time under Microsoft DI once an application actually references <c>Interlocutor.Persistence.EntityFrameworkCore</c> -
/// this two-parameter subclass, fixing <c>TContext</c> to the application's own <see cref="SampleDbContext"/>
/// at compile time, is the fix: register <em>this</em> type instead, via <c>AddBehavior(typeof(SampleUnitOfWorkBehavior&lt;,&gt;))</c>.
/// </remarks>
/// <typeparam name="TRequest">The request type - no marker required, per ADR-0018.</typeparam>
/// <typeparam name="TResponse">The response type returned after pipeline execution.</typeparam>
/// <param name="db">Lazy factory for the <see cref="SampleDbContext"/> instance backing the transaction.</param>
/// <param name="classifier">The outcome classifier used to determine success vs. failure for rollback decisions.</param>
/// <param name="describer">Optional custom outcome describer, passed through unchanged.</param>
internal sealed class SampleUnitOfWorkBehavior<TRequest, TResponse>(
    Lazy<SampleDbContext> db,
    IOutcomeClassifier? classifier = null,
    IResponseOutcomeDescriber? describer = null
) : UnitOfWorkBehavior<TRequest, TResponse, SampleDbContext>(db, classifier, describer);
