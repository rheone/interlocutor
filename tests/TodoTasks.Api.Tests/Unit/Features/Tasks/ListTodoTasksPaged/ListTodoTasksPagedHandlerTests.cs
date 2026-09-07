namespace TodoTasks.Api.Tests.Unit.Features.Tasks.ListTodoTasksPaged;

using System.Linq.Expressions;
using Interlocutor.Persistence.Abstractions;
using NSubstitute;
using TodoTasks.Api.Domain;
using TodoTasks.Api.Features.Tasks.ListTodoTasksPaged;

/// <summary>
/// Unit tests for <see cref="ListTodoTasksPagedHandler"/> - the handler logic in isolation, with
/// <see cref="IDomainRepository{TEntity,TKey}"/> NSubstitute-mocked. No database involved; see
/// <c>Persistence/TodoTaskPersistenceTests</c> for the real-mapping paging round-trip (partial, empty,
/// and boundary pages) and <c>Http/TodoTasksEndpointTests</c> for the full HTTP path.
/// </summary>
public sealed class ListTodoTasksPagedHandlerTests
{
    private readonly IDomainRepository<TodoTask, int> tasks = Substitute.For<IDomainRepository<TodoTask, int>>();
    private readonly ListTodoTasksPagedHandler handler;

    public ListTodoTasksPagedHandlerTests() => this.handler = new ListTodoTasksPagedHandler(this.tasks);

    [Fact]
    public async Task Handle_PassesTheOwnerScopedFilter_AndThePageArguments_ToTheRepository_Test()
    {
        // Arrange - the handler mocked here can't prove the *filter expression itself* excludes another
        // owner's tasks (that's IDomainRepository's own contract, proven for real against a real database
        // in Persistence/TodoTaskPersistenceTests); what this test proves is that the handler passes
        // OwnerId through to GetPagedAsync's filter, forwards the requested page/pageSize unchanged, and
        // maps the returned Page<TodoTask> into a PagedTodoTasksDto that carries the same metadata.
        var ownedByCaller = new TodoTask("Buy milk", "owner-1", null);
        this.tasks.GetPagedAsync(
                Arg.Any<int>(),
                Arg.Any<int>(),
                Arg.Any<Expression<Func<TodoTask, bool>>>(),
                Arg.Any<IReadOnlyList<SortCriterion<TodoTask>>?>(),
                Arg.Any<CancellationToken>()
            )
            .Returns(new Page<TodoTask>([ownedByCaller], PageNumber: 2, PageSize: 5, TotalCount: 6));
        var query = new ListTodoTasksPagedQuery(OwnerId: "owner-1", Page: 2, PageSize: 5);

        // Act
        var result = await this.handler.Handle(query, CancellationToken.None);

        // Assert
        var dto = Assert.IsType<PagedTodoTasksDto>(result.Value);
        var item = Assert.Single(dto.Items);
        Assert.Equal("Buy milk", item.Title);
        Assert.Equal(2, dto.Page);
        Assert.Equal(5, dto.PageSize);
        Assert.Equal(6, dto.TotalCount);
        await this
            .tasks.Received(1)
            .GetPagedAsync(
                2,
                5,
                Arg.Is<Expression<Func<TodoTask, bool>>>(f => FilterMatches(f, ownedByCaller)),
                Arg.Any<IReadOnlyList<SortCriterion<TodoTask>>?>(),
                Arg.Any<CancellationToken>()
            );
    }

    [Fact]
    public async Task Handle_NoMatchingTasks_ReturnsAnEmptyPage_ButStillReportsTheRequestedPageMetadata_Test()
    {
        // Arrange
        this.tasks.GetPagedAsync(
                Arg.Any<int>(),
                Arg.Any<int>(),
                Arg.Any<Expression<Func<TodoTask, bool>>>(),
                Arg.Any<IReadOnlyList<SortCriterion<TodoTask>>?>(),
                Arg.Any<CancellationToken>()
            )
            .Returns(new Page<TodoTask>([], PageNumber: 1, PageSize: 10, TotalCount: 0));
        var query = new ListTodoTasksPagedQuery(OwnerId: "owner-1", Page: 1, PageSize: 10);

        // Act
        var result = await this.handler.Handle(query, CancellationToken.None);

        // Assert
        var dto = Assert.IsType<PagedTodoTasksDto>(result.Value);
        Assert.Empty(dto.Items);
        Assert.Equal(0, dto.TotalCount);
    }

    private static bool FilterMatches(Expression<Func<TodoTask, bool>> filter, TodoTask task) => filter.Compile()(task);
}
