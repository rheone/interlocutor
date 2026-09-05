namespace TodoTasks.Api.Tests.Persistence;

using System.Globalization;
using Interlocutor.CommonKernel;
using Interlocutor.CommonKernel.ResultTypes;
using Interlocutor.Persistence.Abstractions;
using Interlocutor.Persistence.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TodoTasks.Api.Domain;
using TodoTasks.Api.Features.Users.DeleteUser;
using TodoTasks.Api.Persistence;
using TodoTasks.Api.Tests.Http;

/// <summary>
/// Proves <see cref="DeleteUserHandler"/>'s multi-repository cascade actually persists correctly against
/// a real in-memory SQLite database - not just that the right mock calls happened (see
/// <c>Unit/Features/Users/DeleteUser/DeleteUserHandlerTests</c> for that), but that the user and their
/// tasks are genuinely gone afterward, another user's tasks are untouched, and - the load-bearing case -
/// that a failure discovered only after the cascade's writes have already been flushed to the database
/// leaves neither the user nor any task deleted.
/// </summary>
/// <remarks>
/// Boots the real host via <see cref="TodoTasksApiFactory"/> (<c>Program.cs</c>'s own DI wiring), exactly
/// like <c>UserPersistenceTests</c>/<c>TodoTaskPersistenceTests</c>, rather than reconstructing the
/// connection/DbContext registration as a separate, test-only copy.
/// </remarks>
public sealed class DeleteUserCascadeTests : IClassFixture<TodoTasksApiFactory>
{
    private readonly TodoTasksApiFactory factory;

    public DeleteUserCascadeTests(TodoTasksApiFactory factory) => this.factory = factory;

    [Fact]
    public async Task Handle_ADeletedUserWithTasks_RemovesTheUserAndEveryOwnedTask_ButLeavesAnotherUsersTasksAlone_Test()
    {
        // Arrange - seed a user with two tasks, and a second, unrelated user with one task, all via a
        // real DbContext/scope so the seed itself is genuinely committed before the cascade runs.
        int deletedUserId;
        int untouchedUserId;
        using (var seedScope = this.factory.Services.CreateScope())
        {
            var seedContext = seedScope.ServiceProvider.GetRequiredService<TodoTasksDbContext>();
            var userToDelete = new User("Ada Lovelace", $"ada-{Guid.NewGuid():N}@example.com");
            var otherUser = new User("Grace Hopper", $"grace-{Guid.NewGuid():N}@example.com");
            await seedContext.Users.AddRangeAsync([userToDelete, otherUser], TestContext.Current.CancellationToken);
            await seedContext.SaveChangesAsync(TestContext.Current.CancellationToken); // Assigns real ids before they're used as TodoTask.OwnerId below.

            deletedUserId = userToDelete.Id;
            untouchedUserId = otherUser.Id;

            await seedContext.Tasks.AddRangeAsync(
                [
                    new TodoTask("Buy milk", deletedUserId.ToString(CultureInfo.InvariantCulture), null),
                    new TodoTask("Walk the dog", deletedUserId.ToString(CultureInfo.InvariantCulture), null),
                    new TodoTask("Ship COBOL", untouchedUserId.ToString(CultureInfo.InvariantCulture), null),
                ],
                TestContext.Current.CancellationToken
            );
            await seedContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        // Act - the actual cascade, through the real handler AND the real commit-gate behavior, against
        // a fresh scope (mirroring a separate HTTP request). Driving the handler alone would leave
        // EntityFrameworkUnitOfWork.SaveChangesAsync's transaction open but never committed - see
        // UnitOfWorkBehavior's own remarks: commit/rollback happens at pipeline completion, not inside
        // SaveChangesAsync. Wrapping via UnitOfWorkBehavior here is what actually commits it, exactly the
        // way TodoTasksUnitOfWorkBehavior<,> does for every real dispatch in Program.cs.
        using (var actScope = this.factory.Services.CreateScope())
        {
            var context = actScope.ServiceProvider.GetRequiredService<Lazy<TodoTasksDbContext>>();
            var users = actScope.ServiceProvider.GetRequiredService<IDomainRepository<User, int>>();
            var tasks = actScope.ServiceProvider.GetRequiredService<IDomainRepository<TodoTask, int>>();
            var unitOfWork = actScope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var handler = new DeleteUserHandler(users, tasks, unitOfWork);
            var commitGate = new UnitOfWorkBehavior<DeleteUserCommand, Outcome<Success, NotFound>, TodoTasksDbContext>(context);
            var command = new DeleteUserCommand(deletedUserId);

            var result = await commitGate.HandleAsync(
                command,
                () => handler.HandleAsync(command, TestContext.Current.CancellationToken),
                TestContext.Current.CancellationToken
            );
            Assert.True(result.IsSuccess);
        }

        // Assert - a brand new scope sees the committed state.
        using var verifyScope = this.factory.Services.CreateScope();
        var verifyContext = verifyScope.ServiceProvider.GetRequiredService<TodoTasksDbContext>();
        Assert.Null(
            await verifyContext.Users.FirstOrDefaultAsync(u => u.Id == deletedUserId, TestContext.Current.CancellationToken)
        );
        Assert.NotNull(
            await verifyContext.Users.FirstOrDefaultAsync(u => u.Id == untouchedUserId, TestContext.Current.CancellationToken)
        );

        var remainingTasks = await verifyContext.Tasks.ToListAsync(TestContext.Current.CancellationToken);
        Assert.DoesNotContain(remainingTasks, t => t.OwnerId == deletedUserId.ToString(CultureInfo.InvariantCulture));
        Assert.Contains(
            remainingTasks,
            t => t.OwnerId == untouchedUserId.ToString(CultureInfo.InvariantCulture) && t.Title == "Ship COBOL"
        );
    }

    [Fact]
    public async Task Handle_AUserWithNoTasks_StillRemovesTheUser_AgainstARealDatabase_Test()
    {
        // Arrange
        int userId;
        using (var seedScope = this.factory.Services.CreateScope())
        {
            var seedContext = seedScope.ServiceProvider.GetRequiredService<TodoTasksDbContext>();
            var user = new User("No Tasks", $"no-tasks-{Guid.NewGuid():N}@example.com");
            await seedContext.Users.AddAsync(user, TestContext.Current.CancellationToken);
            await seedContext.SaveChangesAsync(TestContext.Current.CancellationToken);
            userId = user.Id;
        }

        // Act - wrapped in the real commit-gate behavior; see the previous test's remarks for why a bare
        // handler.HandleAsync call alone would never actually commit the delete.
        using (var actScope = this.factory.Services.CreateScope())
        {
            var context = actScope.ServiceProvider.GetRequiredService<Lazy<TodoTasksDbContext>>();
            var users = actScope.ServiceProvider.GetRequiredService<IDomainRepository<User, int>>();
            var tasks = actScope.ServiceProvider.GetRequiredService<IDomainRepository<TodoTask, int>>();
            var unitOfWork = actScope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var handler = new DeleteUserHandler(users, tasks, unitOfWork);
            var commitGate = new UnitOfWorkBehavior<DeleteUserCommand, Outcome<Success, NotFound>, TodoTasksDbContext>(context);
            var command = new DeleteUserCommand(userId);

            var result = await commitGate.HandleAsync(
                command,
                () => handler.HandleAsync(command, TestContext.Current.CancellationToken),
                TestContext.Current.CancellationToken
            );
            Assert.True(result.IsSuccess);
        }

        // Assert
        using var verifyScope = this.factory.Services.CreateScope();
        var verifyContext = verifyScope.ServiceProvider.GetRequiredService<TodoTasksDbContext>();
        Assert.Null(await verifyContext.Users.FirstOrDefaultAsync(u => u.Id == userId, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Handle_NoUserWithThatId_ReturnsNotFound_AgainstARealDatabase_Test()
    {
        // Arrange
        using var scope = this.factory.Services.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<IDomainRepository<User, int>>();
        var tasks = scope.ServiceProvider.GetRequiredService<IDomainRepository<TodoTask, int>>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var handler = new DeleteUserHandler(users, tasks, unitOfWork);

        // Act
        var result = await handler.HandleAsync(new DeleteUserCommand(999_999), TestContext.Current.CancellationToken);

        // Assert
        Assert.IsType<NotFound>(result.Value);
    }

    /// <summary>
    /// The load-bearing test: proves a failure discovered only *after* the cascade's deletes have
    /// already been flushed to the database - not merely staged in memory - still leaves nothing
    /// committed, because <see cref="UnitOfWorkBehavior{TRequest,TResponse,TContext}"/> rolls back the
    /// still-open transaction that flush happened inside of.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Why this can't just be "throw partway through the loop."</b> <see cref="DeleteUserHandler"/>
    /// stages every delete purely in memory (EF Core's <c>DbSet.Remove</c> issues no SQL) and only ever
    /// calls <see cref="IUnitOfWork.SaveChangesAsync"/> once, at the very end. An exception thrown before
    /// that single call would leave the database untouched *trivially* - no DELETE statement would ever
    /// have been sent - which would prove far less than the ticket asks for: it wouldn't distinguish "the
    /// cascade is atomic" from "the cascade just never got that far."
    /// </para>
    /// <para>
    /// <b>What this test does instead.</b> <see cref="DeleteUserThenFailHandler"/> runs the exact same
    /// cascade <see cref="DeleteUserHandler"/> does - load the user, delete every owned task, delete the
    /// user, call <see cref="IUnitOfWork.SaveChangesAsync"/> - and only *after* that call (which, per
    /// <see cref="EntityFrameworkUnitOfWork{TContext}"/>'s own remarks, begins a transaction if none is
    /// open and then forwards to <c>DbContext.SaveChangesAsync</c>, physically executing the DELETE
    /// statements against SQLite inside that still-uncommitted transaction) returns a forced
    /// <see cref="Conflict"/> instead of <see cref="Success"/>. That handler is driven directly through
    /// <see cref="UnitOfWorkBehavior{TRequest,TResponse,TContext}"/> - the exact base class
    /// <c>TodoTasksUnitOfWorkBehavior&lt;,&gt;</c> (registered in <c>Program.cs</c> ahead of every real
    /// pipeline dispatch) closes over - so this exercises real, unmodified commit-gate production code,
    /// not a reimplementation of it. <see cref="Conflict"/> is one of the outcome cases
    /// <c>OutcomeClassifierBuilder</c>'s default classifier treats as a failure, so
    /// <see cref="Interlocutor.Persistence.Abstractions.CommitGateDriver"/> takes its rollback branch:
    /// it calls <c>RollbackAsync</c> on the transaction that already had the DELETEs executed against it,
    /// physically undoing them at the database engine level rather than merely never having sent them.
    /// </para>
    /// <para>
    /// A brand new scope afterward proves the point: both the user row and both task rows are still
    /// there, even though real DELETE SQL for all three genuinely ran against the database mid-dispatch.
    /// </para>
    /// </remarks>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task Handle_AFailureDiscoveredAfterTheCascadesFlush_RollsBackEveryDelete_LeavingTheUserAndEveryTaskIntact_Test()
    {
        // Arrange - seed exactly like the success-path test above.
        int userId;
        using (var seedScope = this.factory.Services.CreateScope())
        {
            var seedContext = seedScope.ServiceProvider.GetRequiredService<TodoTasksDbContext>();
            var user = new User("Ada Lovelace", $"ada-{Guid.NewGuid():N}@example.com");
            await seedContext.Users.AddAsync(user, TestContext.Current.CancellationToken);
            await seedContext.SaveChangesAsync(TestContext.Current.CancellationToken);
            userId = user.Id;

            await seedContext.Tasks.AddRangeAsync(
                [
                    new TodoTask("Buy milk", userId.ToString(CultureInfo.InvariantCulture), null),
                    new TodoTask("Walk the dog", userId.ToString(CultureInfo.InvariantCulture), null),
                ],
                TestContext.Current.CancellationToken
            );
            await seedContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        // Act - run DeleteUserThenFailHandler's cascade-then-forced-failure through the real
        // UnitOfWorkBehavior<,,> commit-gate.
        using (var actScope = this.factory.Services.CreateScope())
        {
            var context = actScope.ServiceProvider.GetRequiredService<Lazy<TodoTasksDbContext>>();
            var users = actScope.ServiceProvider.GetRequiredService<IDomainRepository<User, int>>();
            var tasks = actScope.ServiceProvider.GetRequiredService<IDomainRepository<TodoTask, int>>();
            var unitOfWork = actScope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var handler = new DeleteUserThenFailHandler(users, tasks, unitOfWork);
            var commitGate = new UnitOfWorkBehavior<
                DeleteUserThenFailCommand,
                Outcome<Success, NotFound, Conflict>,
                TodoTasksDbContext
            >(context);
            var command = new DeleteUserThenFailCommand(userId);

            var result = await commitGate.HandleAsync(
                command,
                () => handler.HandleAsync(command, TestContext.Current.CancellationToken),
                TestContext.Current.CancellationToken
            );

            // The handler's own forced failure came through unchanged - the commit gate never rewrites
            // outcomes, only decides commit vs. rollback based on them.
            Assert.IsType<Conflict>(result.Value);
        }

        // Assert - a brand new scope proves the DELETEs that genuinely ran were rolled back, not merely
        // never sent.
        using var verifyScope = this.factory.Services.CreateScope();
        var verifyContext = verifyScope.ServiceProvider.GetRequiredService<TodoTasksDbContext>();
        Assert.NotNull(
            await verifyContext.Users.FirstOrDefaultAsync(u => u.Id == userId, TestContext.Current.CancellationToken)
        );
        var remainingTasks = await verifyContext
            .Tasks.Where(t => t.OwnerId == userId.ToString(CultureInfo.InvariantCulture))
            .ToListAsync(TestContext.Current.CancellationToken);
        Assert.Equal(2, remainingTasks.Count);
    }
}
