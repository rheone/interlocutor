namespace TodoTasks.Api.Tests.Unit.Features.Tasks.ListTodoTasks;

using System.Linq.Expressions;
using Interlocutor.Persistence.Abstractions;
using NSubstitute;
using TodoTasks.Api.Domain;
using TodoTasks.Api.Features.Tasks.ListTodoTasks;

/// <summary>
/// Unit tests for <see cref="ListTodoTasksHandler"/> - the handler logic in isolation, with
/// <see cref="IDomainRepository{TEntity,TKey}"/> NSubstitute-mocked. No database involved; see
/// <c>Persistence/TodoTaskPersistenceTests</c> for the real-mapping round-trip and
/// <c>Http/TodoTasksEndpointTests</c> for the full HTTP path.
/// </summary>
public sealed class ListTodoTasksHandlerTests
{
    private readonly IDomainRepository<TodoTask, int> tasks = Substitute.For<IDomainRepository<TodoTask, int>>();
    private readonly ListTodoTasksHandler handler;

    public ListTodoTasksHandlerTests() => this.handler = new ListTodoTasksHandler(this.tasks);

    [Fact]
    public async Task Handle_OnlyReturnsTasksMatchingTheFilterTheRepositoryWasGiven_Test()
    {
        // Arrange - the handler mocked here can't prove the *filter expression itself* excludes another
        // owner's tasks (that's IDomainRepository's own contract, proven for real against a real database
        // in Persistence/TodoTaskPersistenceTests and end to end in Http/TodoTasksEndpointTests); what this
        // test proves is that the handler passes OwnerId through to GetManyAsync's filter at all, and
        // projects exactly what the repository hands back - not some caller-supplied or fixed set.
        var ownedByCaller = new TodoTask("Buy milk", "owner-1", null);
        this.tasks.GetManyAsync(
                Arg.Any<Expression<Func<TodoTask, bool>>>(),
                Arg.Any<IReadOnlyList<SortCriterion<TodoTask>>?>(),
                Arg.Any<CancellationToken>()
            )
            .Returns([ownedByCaller]);
        var query = new ListTodoTasksQuery(OnlyIncomplete: false, OwnerId: "owner-1");

        // Act
        var result = await this.handler.HandleAsync(query, CancellationToken.None);

        // Assert
        var dtos = Assert.IsAssignableFrom<IReadOnlyList<TodoTaskDto>>(result.Value);
        var dto = Assert.Single(dtos);
        Assert.Equal("Buy milk", dto.Title);
        await this
            .tasks.Received(1)
            .GetManyAsync(
                Arg.Is<Expression<Func<TodoTask, bool>>>(f => FilterMatches(f, ownedByCaller)),
                Arg.Any<IReadOnlyList<SortCriterion<TodoTask>>?>(),
                Arg.Any<CancellationToken>()
            );
    }

    [Fact]
    public async Task Handle_NoMatchingTasks_ReturnsAnEmptyList_Test()
    {
        // Arrange
        this.tasks.GetManyAsync(
                Arg.Any<Expression<Func<TodoTask, bool>>>(),
                Arg.Any<IReadOnlyList<SortCriterion<TodoTask>>?>(),
                Arg.Any<CancellationToken>()
            )
            .Returns([]);
        var query = new ListTodoTasksQuery(OnlyIncomplete: false, OwnerId: "owner-1");

        // Act
        var result = await this.handler.HandleAsync(query, CancellationToken.None);

        // Assert
        Assert.Empty(Assert.IsAssignableFrom<IReadOnlyList<TodoTaskDto>>(result.Value));
    }

    [Fact]
    public async Task Handle_OnlyIncompleteTrue_ExcludesCompletedTasksFromTheFilterPassedToTheRepository_Test()
    {
        // Arrange
        var incomplete = new TodoTask("Buy milk", "owner-1", null);
        this.tasks.GetManyAsync(
                Arg.Any<Expression<Func<TodoTask, bool>>>(),
                Arg.Any<IReadOnlyList<SortCriterion<TodoTask>>?>(),
                Arg.Any<CancellationToken>()
            )
            .Returns([incomplete]);
        var query = new ListTodoTasksQuery(OnlyIncomplete: true, OwnerId: "owner-1");

        // Act
        await this.handler.HandleAsync(query, CancellationToken.None);

        // Assert - the filter compiled from OnlyIncomplete: true excludes a completed task for the same
        // owner, whereas OnlyIncomplete: false would include it.
        var completed = new TodoTask("Wash car", "owner-1", null);
        completed.IsCompleted = true;
        await this
            .tasks.Received(1)
            .GetManyAsync(
                Arg.Is<Expression<Func<TodoTask, bool>>>(f => !f.Compile()(completed)),
                Arg.Any<IReadOnlyList<SortCriterion<TodoTask>>?>(),
                Arg.Any<CancellationToken>()
            );
    }

    private static bool FilterMatches(Expression<Func<TodoTask, bool>> filter, TodoTask task) => filter.Compile()(task);
}
