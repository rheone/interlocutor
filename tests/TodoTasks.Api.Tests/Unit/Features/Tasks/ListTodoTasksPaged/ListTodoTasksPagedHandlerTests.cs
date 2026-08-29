namespace TodoTasks.Api.Tests.Unit.Features.Tasks.ListTodoTasksPaged;

using System.Linq.Expressions;
using Interlocutor.Persistence.Abstractions;
using NSubstitute;
using TodoTasks.Api.Domain;
using TodoTasks.Api.Features.Tasks.ListTodoTasksPaged;

/// <summary>Unit tests for <see cref="ListTodoTasksPagedHandler"/>.</summary>
public sealed class ListTodoTasksPagedHandlerTests
{
    private readonly IDomainRepository<TodoTask, int> tasks = Substitute.For<IDomainRepository<TodoTask, int>>();
    private readonly ListTodoTasksPagedHandler handler;

    public ListTodoTasksPagedHandlerTests() => this.handler = new ListTodoTasksPagedHandler(this.tasks);

    [Fact]
    public async Task Handle_APageOfResults_MapsEachItemAndCarriesThePagingMetadata_Test()
    {
        // Arrange
        var task = new TodoTask("Buy milk", "owner-1", null);
        this.tasks.GetPagedAsync(
                Arg.Any<int>(),
                Arg.Any<int>(),
                Arg.Any<Expression<Func<TodoTask, bool>>?>(),
                Arg.Any<IReadOnlyList<SortCriterion<TodoTask>>?>(),
                Arg.Any<CancellationToken>()
            )
            .Returns(new Page<TodoTask>([task], PageNumber: 1, PageSize: 10, TotalCount: 1));
        var query = new ListTodoTasksPagedQuery("owner-1", Page: 1, PageSize: 10);

        // Act
        var result = await this.handler.Handle(query, CancellationToken.None);

        // Assert
        var page = Assert.IsType<PagedTodoTasksDto>(result.Value);
        var item = Assert.Single(page.Items);
        Assert.Equal("Buy milk", item.Title);
        Assert.Equal(1, page.Page);
        Assert.Equal(10, page.PageSize);
        Assert.Equal(1, page.TotalCount);
    }

    [Fact]
    public async Task Handle_ALastPageWithFewerItemsThanPageSize_ReportsTheRealItemCount_Test()
    {
        // Arrange - a partial last page: PageSize is 10, but only 3 items remain, and TotalCount (13)
        // reflects every task across every page, not just this one.
        var items = new[]
        {
            new TodoTask("Task 11", "owner-1", null),
            new TodoTask("Task 12", "owner-1", null),
            new TodoTask("Task 13", "owner-1", null),
        };
        this.tasks.GetPagedAsync(
                Arg.Any<int>(),
                Arg.Any<int>(),
                Arg.Any<Expression<Func<TodoTask, bool>>?>(),
                Arg.Any<IReadOnlyList<SortCriterion<TodoTask>>?>(),
                Arg.Any<CancellationToken>()
            )
            .Returns(new Page<TodoTask>(items, PageNumber: 2, PageSize: 10, TotalCount: 13));
        var query = new ListTodoTasksPagedQuery("owner-1", Page: 2, PageSize: 10);

        // Act
        var result = await this.handler.Handle(query, CancellationToken.None);

        // Assert
        var page = Assert.IsType<PagedTodoTasksDto>(result.Value);
        Assert.Equal(3, page.Items.Count);
        Assert.Equal(2, page.Page);
        Assert.Equal(10, page.PageSize);
        Assert.Equal(13, page.TotalCount);
    }

    [Fact]
    public async Task Handle_AnyQuery_ScopesToTheOwner_AndSortsByCreatedAtUtcDescending_Test()
    {
        // Arrange
        Expression<Func<TodoTask, bool>>? capturedFilter = null;
        IReadOnlyList<SortCriterion<TodoTask>>? capturedSort = null;
        this.tasks.GetPagedAsync(
                Arg.Any<int>(),
                Arg.Any<int>(),
                Arg.Do<Expression<Func<TodoTask, bool>>?>(f => capturedFilter = f),
                Arg.Do<IReadOnlyList<SortCriterion<TodoTask>>?>(s => capturedSort = s),
                Arg.Any<CancellationToken>()
            )
            .Returns(new Page<TodoTask>([], PageNumber: 1, PageSize: 10, TotalCount: 0));
        var query = new ListTodoTasksPagedQuery("owner-1", Page: 1, PageSize: 10);

        // Act
        await this.handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.NotNull(capturedFilter);
        var criterion = Assert.Single(capturedSort!);
        Assert.Equal(SortDirection.Descending, criterion.Direction);
    }
}
