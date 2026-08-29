namespace TodoTasks.Api.Tests.Unit.Features.Tasks.ListTodoTasks;

using System.Linq.Expressions;
using Interlocutor.Persistence.Abstractions;
using NSubstitute;
using TodoTasks.Api.Domain;
using TodoTasks.Api.Features.Tasks.ListTodoTasks;

/// <summary>Unit tests for <see cref="ListTodoTasksHandler"/>.</summary>
public sealed class ListTodoTasksHandlerTests
{
    private readonly IDomainRepository<TodoTask, int> tasks = Substitute.For<IDomainRepository<TodoTask, int>>();
    private readonly ListTodoTasksHandler handler;

    public ListTodoTasksHandlerTests() => this.handler = new ListTodoTasksHandler(this.tasks);

    [Fact]
    public async Task Handle_TasksExist_ReturnsThemAsDtos_Test()
    {
        // Arrange
        var task = new TodoTask("Buy milk", "owner-1", null);
        this.tasks.GetManyAsync(
                Arg.Any<Expression<Func<TodoTask, bool>>?>(),
                Arg.Any<IReadOnlyList<SortCriterion<TodoTask>>?>(),
                Arg.Any<CancellationToken>()
            )
            .Returns([task]);
        var query = new ListTodoTasksQuery(OnlyIncomplete: false);

        // Act
        var result = await this.handler.Handle(query, CancellationToken.None);

        // Assert
        var dtos = Assert.IsAssignableFrom<IReadOnlyList<TodoTaskDto>>(result.Value);
        Assert.Single(dtos);
        Assert.Equal("Buy milk", dtos[0].Title);
    }

    [Fact]
    public async Task Handle_AnyQuery_SortsByCreatedAtUtcDescending_Test()
    {
        // Arrange
        IReadOnlyList<SortCriterion<TodoTask>>? capturedSort = null;
        this.tasks.GetManyAsync(
                Arg.Any<Expression<Func<TodoTask, bool>>?>(),
                Arg.Do<IReadOnlyList<SortCriterion<TodoTask>>?>(s => capturedSort = s),
                Arg.Any<CancellationToken>()
            )
            .Returns([]);
        var query = new ListTodoTasksQuery(OnlyIncomplete: false);

        // Act
        await this.handler.Handle(query, CancellationToken.None);

        // Assert - matches the guide's own "newest first" ordering.
        var criterion = Assert.Single(capturedSort!);
        Assert.Equal(SortDirection.Descending, criterion.Direction);
    }

    [Fact]
    public async Task Handle_OnlyIncompleteTrue_PassesAFilterExpression_Test()
    {
        // Arrange - captures the actual filter argument rather than matching it with Arg.Is, since an
        // Expression<Func<...>>-typed argument makes Arg.Is<T>'s two overloads genuinely ambiguous.
        Expression<Func<TodoTask, bool>>? capturedFilter = null;
        this.tasks.GetManyAsync(
                Arg.Do<Expression<Func<TodoTask, bool>>?>(f => capturedFilter = f),
                Arg.Any<IReadOnlyList<SortCriterion<TodoTask>>?>(),
                Arg.Any<CancellationToken>()
            )
            .Returns([]);
        var query = new ListTodoTasksQuery(OnlyIncomplete: true);

        // Act
        await this.handler.Handle(query, CancellationToken.None);

        // Assert - matches the guide's own "query.OnlyIncomplete ? t => !t.IsCompleted : null" ternary.
        Assert.NotNull(capturedFilter);
    }

    [Fact]
    public async Task Handle_OnlyIncompleteFalse_PassesNoFilter_Test()
    {
        // Arrange
        Expression<Func<TodoTask, bool>>? capturedFilter = null;
        this.tasks.GetManyAsync(
                Arg.Do<Expression<Func<TodoTask, bool>>?>(f => capturedFilter = f),
                Arg.Any<IReadOnlyList<SortCriterion<TodoTask>>?>(),
                Arg.Any<CancellationToken>()
            )
            .Returns([]);
        var query = new ListTodoTasksQuery(OnlyIncomplete: false);

        // Act
        await this.handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Null(capturedFilter);
    }
}
