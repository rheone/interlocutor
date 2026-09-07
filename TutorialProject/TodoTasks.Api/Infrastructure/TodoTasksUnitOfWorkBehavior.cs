namespace TodoTasks.Api.Infrastructure;

using Interlocutor.Abstractions.Pipeline;
using Interlocutor.Abstractions.Pipeline.Classifiers;
using Interlocutor.Persistence.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TodoTasks.Api.Persistence;

/// <summary>
/// Closes EF Core's arity-3 <see cref="UnitOfWorkBehavior{TRequest,TResponse,TContext}"/> over
/// <see cref="TodoTasksDbContext"/>, bringing it back down to arity 2 so it can be registered under
/// Microsoft.Extensions.DependencyInjection.
/// </summary>
/// <remarks>
/// Microsoft.Extensions.DependencyInjection requires an open generic implementation to declare exactly
/// as many open type parameters as the service interface (<see cref="IPipelineBehavior{TRequest,TResponse}"/>,
/// arity 2) it's registered against - <see cref="UnitOfWorkBehavior{TRequest,TResponse,TContext}"/>'s
/// extra <c>TContext</c> parameter breaks that under this container (Autofac has no such restriction, so
/// <c>InterlocutorPipelineBuilder.AddUnitOfWorkBehavior()</c> works there unmodified). This is the
/// workaround documented in documentation/transactions-and-store-integration.md's "EntityFramework Core
/// Integration" section - registered via the general-purpose <c>InterlocutorPipelineBuilder.AddBehavior</c>
/// escape hatch in <c>Program.cs</c> instead of <c>AddUnitOfWorkBehavior()</c>.
/// </remarks>
/// <typeparam name="TRequest">The request type this behavior processes. Unconstrained, matching the base type.</typeparam>
/// <typeparam name="TResponse">The response type returned after pipeline execution.</typeparam>
/// <param name="context">Lazy factory for the scoped <see cref="TodoTasksDbContext"/> backing the transaction.</param>
/// <param name="classifier">The outcome classifier used to label the outcome for the commit debug log line, when <paramref name="describer"/> is not supplied.</param>
/// <param name="describer">Optional custom describer for converting response outcomes to human-readable strings for logging.</param>
/// <param name="logger">Optional logger for the commit/rollback decision.</param>
internal sealed class TodoTasksUnitOfWorkBehavior<TRequest, TResponse>(
    Lazy<TodoTasksDbContext> context,
    IOutcomeClassifier? classifier = null,
    IResponseOutcomeDescriber? describer = null,
    ILogger<UnitOfWorkBehavior<TRequest, TResponse, TodoTasksDbContext>>? logger = null
) : UnitOfWorkBehavior<TRequest, TResponse, TodoTasksDbContext>(context, classifier, describer, logger);
