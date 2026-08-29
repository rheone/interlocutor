namespace TodoTasks.Api.Tests.Persistence;

using global::NHibernate;
using Interlocutor.Persistence.NHibernate;
using NSubstitute;
using TodoTasks.Api.Domain;
using TodoTasks.Api.Features.Tasks.SyncAwareCompleteTodoTask;
using TodoTasks.Api.Infrastructure;

/// <summary>
/// Proves <see cref="SyncAwareCompleteTodoTaskHandler"/>'s <c>IUnitOfWork.Cancel</c> path against a real
/// in-memory SQLite database - not just that the right mock calls happened (see
/// <c>Unit/Features/Tasks/SyncAwareCompleteTodoTask/SyncAwareCompleteTodoTaskHandlerTests</c> for that),
/// but that the staged completion genuinely never reaches the database.
/// </summary>
public sealed class SyncAwareCompleteTodoTaskRollbackTests : IClassFixture<PersistenceFixture>
{
    private readonly PersistenceFixture fixture;

    public SyncAwareCompleteTodoTaskRollbackTests(PersistenceFixture fixture) => this.fixture = fixture;

    [Fact]
    public async Task Handle_AlreadyCompletedElsewhere_ReturnsSuccess_ButLeavesTheDatabaseRowUncompleted_Test()
    {
        // Arrange - seed one incomplete task, committed for real (same pattern DeleteUserCascadeTests
        // uses - a plain Flush is enough since nothing else has an open transaction on this connection yet).
        int taskId;
        using (var seedSession = this.fixture.OpenSession())
        {
            var task = new TodoTask("Buy milk", "owner-1", null);
            await seedSession.SaveAsync(task, TestContext.Current.CancellationToken);
            await seedSession.FlushAsync(TestContext.Current.CancellationToken);
            taskId = task.Id;
        }

        // Act - a fresh session/dispatch, exactly like a real request. The external sync client reports
        // the task was already completed elsewhere, so the handler stages the completion and then
        // abandons it via Cancel rather than committing.
        TodoTaskDto? returnedDto;
        using (var session = this.fixture.OpenSession())
        {
            var lazySession = new Lazy<global::NHibernate.ISession>(() => session);
            var tasks = new NHibernateRepository<TodoTask, int>(lazySession, t => t.Id);
            var unitOfWork = new NHibernateUnitOfWork(lazySession);
            var externalSync = Substitute.For<IExternalTaskSyncClient>();
            externalSync.WasAlreadyCompletedElsewhereAsync(taskId, Arg.Any<CancellationToken>()).Returns(true);
            var handler = new SyncAwareCompleteTodoTaskHandler(tasks, unitOfWork, externalSync);

            var result = await handler.Handle(
                new SyncAwareCompleteTodoTaskCommand(taskId),
                TestContext.Current.CancellationToken
            );
            returnedDto = Assert.IsType<TodoTaskDto>(result.Value);

            // Nothing left open for this session's Dispose() to roll back on its own - IUnitOfWork.Cancel
            // already did that (or, as here, never opened a transaction at all, since the staged change
            // was never flushed - see the handler's own remarks). This assertion is the point of the
            // test, not incidental cleanup.
            Assert.False(session.GetCurrentTransaction() is { IsActive: true });
        }

        // Assert - the handler reported the task as completed (correct: it is, just not through this
        // write)...
        Assert.True(returnedDto.IsCompleted);

        // ...but a brand new session proves the database row itself was never touched.
        using var verifySession = this.fixture.OpenSession();
        var persisted = await verifySession.GetAsync<TodoTask>(taskId, TestContext.Current.CancellationToken);
        Assert.NotNull(persisted);
        Assert.False(persisted.IsCompleted);
    }

    [Fact]
    public async Task Handle_NotCompletedElsewhere_ActuallyPersistsTheCompletion_Test()
    {
        // Arrange
        int taskId;
        using (var seedSession = this.fixture.OpenSession())
        {
            var task = new TodoTask("Buy milk", "owner-1", null);
            await seedSession.SaveAsync(task, TestContext.Current.CancellationToken);
            await seedSession.FlushAsync(TestContext.Current.CancellationToken);
            taskId = task.Id;
        }

        // Act - this time the external system never completed it, so the handler commits for real. As
        // in DeleteUserCascadeTests, the handler's own SaveChangesAsync only flushes and begins a
        // transaction - UnitOfWorkBehavior's commit is what a real pipeline dispatch would do at egress,
        // so this test commits explicitly in its place.
        using (var session = this.fixture.OpenSession())
        {
            var lazySession = new Lazy<global::NHibernate.ISession>(() => session);
            var tasks = new NHibernateRepository<TodoTask, int>(lazySession, t => t.Id);
            var unitOfWork = new NHibernateUnitOfWork(lazySession);
            var externalSync = Substitute.For<IExternalTaskSyncClient>();
            externalSync.WasAlreadyCompletedElsewhereAsync(taskId, Arg.Any<CancellationToken>()).Returns(false);
            var handler = new SyncAwareCompleteTodoTaskHandler(tasks, unitOfWork, externalSync);

            await handler.Handle(new SyncAwareCompleteTodoTaskCommand(taskId), TestContext.Current.CancellationToken);
            session.CommitIfActive();
        }

        // Assert
        using var verifySession = this.fixture.OpenSession();
        var persisted = await verifySession.GetAsync<TodoTask>(taskId, TestContext.Current.CancellationToken);
        Assert.True(persisted.IsCompleted);
    }
}
