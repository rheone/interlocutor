namespace Interlocutor.IntegrationTests;

using Interlocutor.Persistence.EntityFrameworkCore;

/// <summary>
/// Closes <see cref="UnitOfWorkBehavior{TRequest,TResponse,TContext}"/>'s <c>TContext</c> parameter over
/// <see cref="IntegrationTestDbContext"/>, bringing its arity back down to the two parameters
/// <c>IPipelineBehavior&lt;TRequest,TResponse&gt;</c> declares.
/// </summary>
/// <remarks>
/// This is the documented workaround for registering EF Core's <c>UnitOfWorkBehavior</c> under
/// Microsoft.Extensions.DependencyInjection - see <c>InterlocutorPipelineBuilder.AddUnitOfWorkBehavior</c>'s
/// remarks and documentation/transactions-and-store-integration.md's "Autofac/Microsoft DI" warning.
/// <c>AddUnitOfWorkBehavior()</c> itself throws at container-build time here, since Microsoft DI cannot
/// register an open generic implementation whose arity doesn't match its service interface's - this subclass
/// is registered instead, through the general-purpose <c>InterlocutorPipelineBuilder.AddBehavior</c> escape
/// hatch.
/// </remarks>
/// <param name="db">Lazy factory for the <see cref="IntegrationTestDbContext"/> instance backing the transaction.</param>
internal sealed class TestDbContextUnitOfWorkBehavior<TRequest, TResponse>(Lazy<IntegrationTestDbContext> db)
    : UnitOfWorkBehavior<TRequest, TResponse, IntegrationTestDbContext>(db);
