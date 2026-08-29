namespace TodoTasks.Api.Tests.Persistence;

using Interlocutor.Persistence.NHibernate;
using TodoTasks.Api.Domain;
using TodoTasks.Api.Features.Users.DeleteUser;

/// <summary>
/// Proves <see cref="DeleteUserHandler"/>'s multi-repository cascade actually persists correctly
/// against a real in-memory SQLite database - not just that the right mock calls happened (see
/// <c>Unit/Features/Users/DeleteUser/DeleteUserHandlerTests</c> for that), but that the user and
/// their tasks are genuinely gone afterward, and that a different user's tasks are untouched.
/// </summary>
public sealed class DeleteUserCascadeTests : IClassFixture<PersistenceFixture>
{
    private readonly PersistenceFixture fixture;

    public DeleteUserCascadeTests(PersistenceFixture fixture) => this.fixture = fixture;

    [Fact]
    public async Task Handle_ADeletedUserWithTasks_RemovesTheUserAndEveryOwnedTask_ButLeavesOtherUsersTasksAlone_Test()
    {
        // Arrange - seed a user with two tasks, and a second, unrelated user with one task, all via
        // real repositories/unit of work against the fixture's real in-memory database.
        // Seeding flushes directly on the session, without an explicit transaction - the same pattern
        // UserMapTests/TodoTaskMapTests use - so SQLite's autocommit-outside-a-transaction behavior makes
        // this data visible to every later session against the shared-cache in-memory database, with
        // nothing left open that a later Dispose() could roll back.
        int deletedUserId;
        int untouchedUserId;
        using (var session = this.fixture.OpenSession())
        {
            var lazySession = new Lazy<global::NHibernate.ISession>(() => session);
            var users = new NHibernateRepository<User, int>(lazySession, u => u.Id);
            var tasks = new NHibernateRepository<TodoTask, int>(lazySession, t => t.Id);

            var userToDelete = new User("Ada Lovelace", "ada@example.com");
            var otherUser = new User("Grace Hopper", "grace@example.com");
            await users.InsertAsync(userToDelete, TestContext.Current.CancellationToken);
            await users.InsertAsync(otherUser, TestContext.Current.CancellationToken);
            await session.FlushAsync(TestContext.Current.CancellationToken); // Assign real ids before they're used as TodoTask.OwnerId below.

            deletedUserId = userToDelete.Id;
            untouchedUserId = otherUser.Id;

            await tasks.InsertAsync(
                new TodoTask("Buy milk", deletedUserId.ToString(), null),
                TestContext.Current.CancellationToken
            );
            await tasks.InsertAsync(
                new TodoTask("Walk the dog", deletedUserId.ToString(), null),
                TestContext.Current.CancellationToken
            );
            await tasks.InsertAsync(
                new TodoTask("Ship COBOL", untouchedUserId.ToString(), null),
                TestContext.Current.CancellationToken
            );
            await session.FlushAsync(TestContext.Current.CancellationToken);
        }

        // Act - the actual cascade, through the real handler, against a fresh session (mirroring a
        // separate HTTP request). DeleteUserHandler's IUnitOfWork.SaveChangesAsync only begins a
        // transaction and flushes - see NHibernateUnitOfWork's own remarks - it deliberately leaves the
        // commit to UnitOfWorkBehavior at the end of a real pipeline dispatch. Calling the handler
        // directly bypasses that pipeline, so this test commits explicitly in its place; without it, the
        // session's Dispose() below would roll the whole cascade back and the assertions after would
        // fail for a misleading reason.
        using (var session = this.fixture.OpenSession())
        {
            var lazySession = new Lazy<global::NHibernate.ISession>(() => session);
            var unitOfWork = new NHibernateUnitOfWork(lazySession);
            var users = new NHibernateRepository<User, int>(lazySession, u => u.Id);
            var tasks = new NHibernateRepository<TodoTask, int>(lazySession, t => t.Id);
            var handler = new DeleteUserHandler(users, tasks, unitOfWork);

            var result = await handler.Handle(new DeleteUserCommand(deletedUserId), TestContext.Current.CancellationToken);
            Assert.True(result.IsSuccess);

            session.CommitIfActive();
        }

        // Assert - a brand new session sees the committed state.
        using (var session = this.fixture.OpenSession())
        {
            Assert.Null(session.Get<User>(deletedUserId));
            Assert.NotNull(session.Get<User>(untouchedUserId));

            var remainingTasks = session.Query<TodoTask>().ToList();
            Assert.DoesNotContain(remainingTasks, t => t.OwnerId == deletedUserId.ToString());
            Assert.Contains(remainingTasks, t => t.OwnerId == untouchedUserId.ToString() && t.Title == "Ship COBOL");
        }
    }
}
