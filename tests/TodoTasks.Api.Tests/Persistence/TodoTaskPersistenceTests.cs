namespace TodoTasks.Api.Tests.Persistence;

using Interlocutor.Abstractions.Core;
using Interlocutor.CommonKernel.ResultTypes;
using Interlocutor.Persistence.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using TodoTasks.Api.Domain;
using TodoTasks.Api.Features.Tasks.CompleteTodoTask;
using TodoTasks.Api.Features.Tasks.GetTodoTaskById;
using TodoTasks.Api.Features.Tasks.ListTodoTasks;
using TodoTasks.Api.Features.Tasks.ReopenTodoTask;
using TodoTasks.Api.Features.Tasks.UpdateTodoTask;
using TodoTasks.Api.Persistence;
using TodoTasks.Api.Tests.Http;

/// <summary>
/// Proves <see cref="TodoTasksDbContext"/>'s <see cref="TodoTask"/> mapping actually round-trips
/// through a real in-memory SQLite database - the kind of mapping mistake (a wrong column name, a
/// missing <c>IsRequired()</c>, an <c>OwnerId</c> that accidentally became a foreign key, a unique
/// index that didn't actually take effect or that isn't scoped the way it should be)
/// <c>Unit/Features/Tasks/CreateTodoTask/*</c>'s NSubstitute-mocked handler tests can never catch.
/// </summary>
/// <remarks>
/// Boots the real host via <see cref="TodoTasksApiFactory"/> (<c>Program.cs</c>'s own DI wiring), exactly
/// like <c>UserPersistenceTests</c>, rather than reconstructing the connection/DbContext registration as
/// a separate, test-only copy.
/// </remarks>
public sealed class TodoTaskPersistenceTests : IClassFixture<TodoTasksApiFactory>
{
    private readonly TodoTasksApiFactory factory;

    public TodoTaskPersistenceTests(TodoTasksApiFactory factory) => this.factory = factory;

    [Fact]
    public async Task SaveThenGet_ATodoTask_RoundTripsEveryMappedMember_Test()
    {
        // Arrange
        using var writeScope = this.factory.Services.CreateScope();
        using var readScope = this.factory.Services.CreateScope();
        var task = new TodoTask("Buy milk", "owner-1", "2%, please");

        // Act - insert and save in one scope's DbContext, then read back through a second scope's
        // DbContext opened against the same kept-alive connection, the same way a real dispatch's insert
        // and a later request's read are two different scoped DbContext instances sharing one physical
        // database.
        var writeContext = writeScope.ServiceProvider.GetRequiredService<TodoTasksDbContext>();
        await writeContext.Tasks.AddAsync(task, TestContext.Current.CancellationToken);
        await writeContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var readContext = readScope.ServiceProvider.GetRequiredService<TodoTasksDbContext>();
        var reloaded = await readContext.Tasks.FirstOrDefaultAsync(t => t.Id == task.Id, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(reloaded);
        Assert.Equal(task.Id, reloaded.Id);
        Assert.Equal("Buy milk", reloaded.Title);
        Assert.Equal("2%, please", reloaded.Notes);
        Assert.Equal("owner-1", reloaded.OwnerId);
        Assert.False(reloaded.IsCompleted);
        Assert.Equal(task.CreatedAtUtc, reloaded.CreatedAtUtc);
    }

    [Fact]
    public async Task SaveChanges_TwoTasksWithTheSameTitleForTheSameOwner_ViolatesTheUniqueIndex_Test()
    {
        // Arrange - this exercises the schema-level unique index OnModelCreating declares directly on
        // (OwnerId, Title), bypassing CreateTodoTaskHandler's own pre-check query entirely, to prove the
        // index itself is real rather than merely assumed.
        using var scope = this.factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TodoTasksDbContext>();
        await context.Tasks.AddAsync(new TodoTask("Duplicate title", "owner-1", null), TestContext.Current.CancellationToken);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        await context.Tasks.AddAsync(new TodoTask("Duplicate title", "owner-1", null), TestContext.Current.CancellationToken);

        // Act & Assert
        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task SaveChanges_TwoTasksWithTheSameTitleForDifferentOwners_Succeeds_Test()
    {
        // Arrange - the unique index is scoped to (OwnerId, Title), not Title alone: two different
        // owners must each be free to have their own "Buy milk", matching CreateTodoTaskHandler's own
        // per-owner duplicate-title semantics rather than a global one.
        using var scope = this.factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TodoTasksDbContext>();
        await context.Tasks.AddAsync(new TodoTask("Shared title", "owner-1", null), TestContext.Current.CancellationToken);
        await context.Tasks.AddAsync(new TodoTask("Shared title", "owner-2", null), TestContext.Current.CancellationToken);

        // Act
        var savedCount = await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(2, savedCount);
    }

    [Fact]
    public async Task GetTodoTaskByIdHandler_TaskExistsButIsOwnedBySomeoneElse_ReturnsNotFound_AgainstARealDatabase_Test()
    {
        // Arrange - proves the cross-owner NotFound behavior holds against the real EF Core-backed
        // repository and a real SQLite database, not just against a mock that echoes back whatever it's
        // told to (see the mocked equivalent in Unit/Features/Tasks/GetTodoTaskById). Owner ids are
        // unique per test (not a fixed "owner-1"/"owner-2") because TodoTasksApiFactory's database is
        // shared across every test in this class via IClassFixture.
        using var scope = this.factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TodoTasksDbContext>();
        var owner = $"owner-{Guid.NewGuid():N}";
        var task = new TodoTask("Buy milk", owner, null);
        await context.Tasks.AddAsync(task, TestContext.Current.CancellationToken);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var tasks = scope.ServiceProvider.GetRequiredService<IDomainRepository<TodoTask, int>>();
        var handler = new GetTodoTaskByIdHandler(tasks);

        // Act
        var result = await handler.Handle(
            new GetTodoTaskByIdQuery(task.Id, $"someone-else-{Guid.NewGuid():N}"),
            TestContext.Current.CancellationToken
        );

        // Assert
        Assert.IsType<NotFound>(result.Value);
    }

    [Fact]
    public async Task GetTodoTaskByIdHandler_TaskExistsAndIsOwnedByTheCaller_ReturnsItsDto_AgainstARealDatabase_Test()
    {
        // Arrange
        using var scope = this.factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TodoTasksDbContext>();
        var owner = $"owner-{Guid.NewGuid():N}";
        var task = new TodoTask("Buy milk", owner, null);
        await context.Tasks.AddAsync(task, TestContext.Current.CancellationToken);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var tasks = scope.ServiceProvider.GetRequiredService<IDomainRepository<TodoTask, int>>();
        var handler = new GetTodoTaskByIdHandler(tasks);

        // Act
        var result = await handler.Handle(new GetTodoTaskByIdQuery(task.Id, owner), TestContext.Current.CancellationToken);

        // Assert
        var dto = Assert.IsType<TodoTaskDto>(result.Value);
        Assert.Equal(task.Id, dto.Id);
    }

    [Fact]
    public async Task ListTodoTasksHandler_TwoOwnersHaveTasks_OnlyReturnsTheRequestedOwnersTasks_AgainstARealDatabase_Test()
    {
        // Arrange - ListTodoTasksHandlerTests proves the handler passes OwnerId through to the
        // repository's filter against a mock; this proves that filter expression actually translates to
        // SQL that scopes the result set correctly against a real database. Owner ids are unique per
        // test - see the remarks on the GetTodoTaskByIdHandler test above for why.
        using var scope = this.factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TodoTasksDbContext>();
        var requestedOwner = $"owner-{Guid.NewGuid():N}";
        var otherOwner = $"owner-{Guid.NewGuid():N}";
        await context.Tasks.AddRangeAsync(
            [
                new TodoTask("Requested owner, task A", requestedOwner, null),
                new TodoTask("Requested owner, task B", requestedOwner, null),
                new TodoTask("Other owner, task A", otherOwner, null),
            ],
            TestContext.Current.CancellationToken
        );
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var tasks = scope.ServiceProvider.GetRequiredService<IDomainRepository<TodoTask, int>>();
        var handler = new ListTodoTasksHandler(tasks);

        // Act
        var result = await handler.Handle(
            new ListTodoTasksQuery(OnlyIncomplete: false, OwnerId: requestedOwner),
            TestContext.Current.CancellationToken
        );

        // Assert
        var dtos = Assert.IsAssignableFrom<IReadOnlyList<TodoTaskDto>>(result.Value);
        Assert.Equal(2, dtos.Count);
        Assert.All(dtos, dto => Assert.StartsWith("Requested owner,", dto.Title, StringComparison.Ordinal));
    }

    [Fact]
    public async Task UpdateTodoTaskHandler_TaskExistsAndIsOwnedByTheCaller_PersistsTheChange_AgainstARealDatabase_Test()
    {
        // Arrange - proves the update actually reaches the database, not just the in-memory entity the
        // mocked handler test (Unit/Features/Tasks/UpdateTodoTask) mutates: a second, independent scope's
        // DbContext re-reads the row afterward, against the same kept-alive in-memory SQLite connection
        // every scope in this fixture shares (see TodoTasksApiFactory's own remarks).
        using var writeScope = this.factory.Services.CreateScope();
        var writeContext = writeScope.ServiceProvider.GetRequiredService<TodoTasksDbContext>();
        var owner = $"owner-{Guid.NewGuid():N}";
        var task = new TodoTask("Buy milk", owner, "2%, please");
        await writeContext.Tasks.AddAsync(task, TestContext.Current.CancellationToken);
        await writeContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        var tasks = writeScope.ServiceProvider.GetRequiredService<IDomainRepository<TodoTask, int>>();
        var unitOfWork = writeScope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var handler = new UpdateTodoTaskHandler(tasks, unitOfWork);

        // Act
        var result = await handler.Handle(
            new UpdateTodoTaskCommand(task.Id, "Buy oat milk", "Oat milk this time", owner),
            TestContext.Current.CancellationToken
        );

        // Assert
        var dto = Assert.IsType<TodoTaskDto>(result.Value);
        Assert.Equal("Buy oat milk", dto.Title);
        using var readScope = this.factory.Services.CreateScope();
        var readContext = readScope.ServiceProvider.GetRequiredService<TodoTasksDbContext>();
        var reloaded = await readContext.Tasks.FirstOrDefaultAsync(t => t.Id == task.Id, TestContext.Current.CancellationToken);
        Assert.NotNull(reloaded);
        Assert.Equal("Buy oat milk", reloaded.Title);
        Assert.Equal("Oat milk this time", reloaded.Notes);
    }

    [Fact]
    public async Task UpdateTodoTaskHandler_TaskExistsButIsOwnedBySomeoneElse_ReturnsForbidden_AndDoesNotPersistTheChange_AgainstARealDatabase_Test()
    {
        // Arrange - proves the owner-scoping in UpdateTodoTaskHandler.Authorize holds against a real
        // repository/database, and that a denied update never reaches SaveChangesAsync at all (the row
        // reloaded below still has its original title).
        using var writeScope = this.factory.Services.CreateScope();
        var writeContext = writeScope.ServiceProvider.GetRequiredService<TodoTasksDbContext>();
        var owner = $"owner-{Guid.NewGuid():N}";
        var task = new TodoTask("Buy milk", owner, null);
        await writeContext.Tasks.AddAsync(task, TestContext.Current.CancellationToken);
        await writeContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        var tasks = writeScope.ServiceProvider.GetRequiredService<IDomainRepository<TodoTask, int>>();
        var unitOfWork = writeScope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var handler = new UpdateTodoTaskHandler(tasks, unitOfWork);

        // Act
        var result = await handler.Handle(
            new UpdateTodoTaskCommand(task.Id, "Hijacked title", null, $"someone-else-{Guid.NewGuid():N}"),
            TestContext.Current.CancellationToken
        );

        // Assert
        Assert.IsType<Forbidden>(result.Value);
        using var readScope = this.factory.Services.CreateScope();
        var readContext = readScope.ServiceProvider.GetRequiredService<TodoTasksDbContext>();
        var reloaded = await readContext.Tasks.FirstOrDefaultAsync(t => t.Id == task.Id, TestContext.Current.CancellationToken);
        Assert.NotNull(reloaded);
        Assert.Equal("Buy milk", reloaded.Title);
    }

    [Fact]
    public async Task UpdateTodoTaskHandler_NoTaskWithThatId_ReturnsNotFound_AgainstARealDatabase_Test()
    {
        // Arrange
        using var scope = this.factory.Services.CreateScope();
        var tasks = scope.ServiceProvider.GetRequiredService<IDomainRepository<TodoTask, int>>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var handler = new UpdateTodoTaskHandler(tasks, unitOfWork);

        // Act
        var result = await handler.Handle(
            new UpdateTodoTaskCommand(999_999, "Doesn't matter", null, $"owner-{Guid.NewGuid():N}"),
            TestContext.Current.CancellationToken
        );

        // Assert
        Assert.IsType<NotFound>(result.Value);
    }

    [Fact]
    public async Task CompleteTodoTaskHandler_TaskExistsAndIsOwnedByTheCaller_PersistsTheCompletion_AgainstARealDatabase_Test()
    {
        // Arrange - proves the completion actually reaches the database, not just the in-memory entity
        // the mocked handler test (Unit/Features/Tasks/CompleteTodoTask) mutates.
        using var writeScope = this.factory.Services.CreateScope();
        var writeContext = writeScope.ServiceProvider.GetRequiredService<TodoTasksDbContext>();
        var owner = $"owner-{Guid.NewGuid():N}";
        var task = new TodoTask("Buy milk", owner, null);
        await writeContext.Tasks.AddAsync(task, TestContext.Current.CancellationToken);
        await writeContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        var tasks = writeScope.ServiceProvider.GetRequiredService<IDomainRepository<TodoTask, int>>();
        var unitOfWork = writeScope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var handler = new CompleteTodoTaskHandler(tasks, unitOfWork, Substitute.For<IPublisher>());

        // Act
        var result = await handler.Handle(new CompleteTodoTaskCommand(task.Id, owner), TestContext.Current.CancellationToken);

        // Assert
        var dto = Assert.IsType<TodoTaskDto>(result.Value);
        Assert.True(dto.IsCompleted);
        using var readScope = this.factory.Services.CreateScope();
        var readContext = readScope.ServiceProvider.GetRequiredService<TodoTasksDbContext>();
        var reloaded = await readContext.Tasks.FirstOrDefaultAsync(t => t.Id == task.Id, TestContext.Current.CancellationToken);
        Assert.NotNull(reloaded);
        Assert.True(reloaded.IsCompleted);
    }

    [Fact]
    public async Task CompleteTodoTaskHandler_TaskExistsButIsOwnedBySomeoneElse_ReturnsForbidden_AndDoesNotPersistTheCompletion_AgainstARealDatabase_Test()
    {
        // Arrange - proves the owner-scoping in CompleteTodoTaskHandler holds against a real
        // repository/database, and that a denied completion never reaches SaveChangesAsync at all.
        using var writeScope = this.factory.Services.CreateScope();
        var writeContext = writeScope.ServiceProvider.GetRequiredService<TodoTasksDbContext>();
        var owner = $"owner-{Guid.NewGuid():N}";
        var task = new TodoTask("Buy milk", owner, null);
        await writeContext.Tasks.AddAsync(task, TestContext.Current.CancellationToken);
        await writeContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        var tasks = writeScope.ServiceProvider.GetRequiredService<IDomainRepository<TodoTask, int>>();
        var unitOfWork = writeScope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var handler = new CompleteTodoTaskHandler(tasks, unitOfWork, Substitute.For<IPublisher>());

        // Act
        var result = await handler.Handle(
            new CompleteTodoTaskCommand(task.Id, $"someone-else-{Guid.NewGuid():N}"),
            TestContext.Current.CancellationToken
        );

        // Assert
        Assert.IsType<Forbidden>(result.Value);
        using var readScope = this.factory.Services.CreateScope();
        var readContext = readScope.ServiceProvider.GetRequiredService<TodoTasksDbContext>();
        var reloaded = await readContext.Tasks.FirstOrDefaultAsync(t => t.Id == task.Id, TestContext.Current.CancellationToken);
        Assert.NotNull(reloaded);
        Assert.False(reloaded.IsCompleted);
    }

    [Fact]
    public async Task ReopenTodoTaskHandler_TaskExistsAndIsOwnedByTheCaller_PersistsTheReopen_AgainstARealDatabase_Test()
    {
        // Arrange - proves the reopen actually reaches the database, not just the in-memory entity the
        // mocked handler test (Unit/Features/Tasks/ReopenTodoTask) mutates.
        using var writeScope = this.factory.Services.CreateScope();
        var writeContext = writeScope.ServiceProvider.GetRequiredService<TodoTasksDbContext>();
        var owner = $"owner-{Guid.NewGuid():N}";
        var task = new TodoTask("Buy milk", owner, null) { IsCompleted = true };
        await writeContext.Tasks.AddAsync(task, TestContext.Current.CancellationToken);
        await writeContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        var tasks = writeScope.ServiceProvider.GetRequiredService<IDomainRepository<TodoTask, int>>();
        var unitOfWork = writeScope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var handler = new ReopenTodoTaskHandler(tasks, unitOfWork);

        // Act
        var result = await handler.Handle(new ReopenTodoTaskCommand(task.Id, owner), TestContext.Current.CancellationToken);

        // Assert
        var dto = Assert.IsType<TodoTaskDto>(result.Value);
        Assert.False(dto.IsCompleted);
        using var readScope = this.factory.Services.CreateScope();
        var readContext = readScope.ServiceProvider.GetRequiredService<TodoTasksDbContext>();
        var reloaded = await readContext.Tasks.FirstOrDefaultAsync(t => t.Id == task.Id, TestContext.Current.CancellationToken);
        Assert.NotNull(reloaded);
        Assert.False(reloaded.IsCompleted);
    }

    [Fact]
    public async Task ReopenTodoTaskHandler_TaskExistsButIsOwnedBySomeoneElse_ReturnsForbidden_AndDoesNotPersistTheReopen_AgainstARealDatabase_Test()
    {
        // Arrange - proves the owner-scoping in ReopenTodoTaskHandler holds against a real
        // repository/database, and that a denied reopen never reaches SaveChangesAsync at all.
        using var writeScope = this.factory.Services.CreateScope();
        var writeContext = writeScope.ServiceProvider.GetRequiredService<TodoTasksDbContext>();
        var owner = $"owner-{Guid.NewGuid():N}";
        var task = new TodoTask("Buy milk", owner, null) { IsCompleted = true };
        await writeContext.Tasks.AddAsync(task, TestContext.Current.CancellationToken);
        await writeContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        var tasks = writeScope.ServiceProvider.GetRequiredService<IDomainRepository<TodoTask, int>>();
        var unitOfWork = writeScope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var handler = new ReopenTodoTaskHandler(tasks, unitOfWork);

        // Act
        var result = await handler.Handle(
            new ReopenTodoTaskCommand(task.Id, $"someone-else-{Guid.NewGuid():N}"),
            TestContext.Current.CancellationToken
        );

        // Assert
        Assert.IsType<Forbidden>(result.Value);
        using var readScope = this.factory.Services.CreateScope();
        var readContext = readScope.ServiceProvider.GetRequiredService<TodoTasksDbContext>();
        var reloaded = await readContext.Tasks.FirstOrDefaultAsync(t => t.Id == task.Id, TestContext.Current.CancellationToken);
        Assert.NotNull(reloaded);
        Assert.True(reloaded.IsCompleted);
    }
}
