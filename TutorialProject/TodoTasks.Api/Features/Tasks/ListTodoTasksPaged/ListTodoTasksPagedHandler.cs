namespace TodoTasks.Api.Features.Tasks.ListTodoTasksPaged;

using Interlocutor.Abstractions.Queries;
using Interlocutor.CommonKernel;
using Interlocutor.CommonKernel.ResultTypes;
using Interlocutor.Persistence.Abstractions;
using TodoTasks.Api.Domain;

/// <summary>Handles <see cref="ListTodoTasksPagedQuery"/> via <see cref="IDomainRepository{TEntity,TKey}.GetPagedAsync"/>.</summary>
/// <remarks>
/// Sorted by <see cref="TodoTask.Id"/> descending, not <see cref="TodoTask.CreatedAtUtc"/> - unlike
/// <see cref="ListTodoTasks.ListTodoTasksHandler"/>'s own client-side sort (which sidesteps this by
/// sorting an already-materialized list), <see cref="IDomainRepository{TEntity,TKey}.GetPagedAsync"/>
/// pushes its <c>sort</c> parameter into the database query alongside <c>Skip</c>/<c>Take</c> - paging
/// only makes sense applied after ordering, and both have to happen in the database, not in memory, for
/// the requested page to be correct. EF Core's SQLite provider cannot translate an <c>ORDER BY</c> over a
/// <see cref="DateTimeOffset"/> column into SQL (SQLite has no native <see cref="DateTimeOffset"/> type),
/// so ordering by <see cref="TodoTask.Id"/> instead - which increases in creation order, since it is
/// assigned by the database on insert - gives the same newest-first result without hitting that
/// translation gap.
/// </remarks>
public sealed class ListTodoTasksPagedHandler(IDomainRepository<TodoTask, int> tasks)
    : IQueryHandler<ListTodoTasksPagedQuery, Outcome<PagedTodoTasksDto, Failure>>
{
    /// <inheritdoc/>
    public Task<Outcome<PagedTodoTasksDto, Failure>> Handle(ListTodoTasksPagedQuery query, CancellationToken ct) =>
        this.HandleAsync(query, ct);

    /// <inheritdoc/>
    public async Task<Outcome<PagedTodoTasksDto, Failure>> HandleAsync(ListTodoTasksPagedQuery query, CancellationToken ct)
    {
        var page = await tasks
            .GetPagedAsync(
                query.Page,
                query.PageSize,
                filter: t => t.OwnerId == query.OwnerId,
                sort: [new SortCriterion<TodoTask>(t => t.Id, SortDirection.Descending)],
                cancellationToken: ct
            )
            .ConfigureAwait(false);

        return new PagedTodoTasksDto(
            page.Items.Select(TodoTaskDto.FromEntity).ToList(),
            page.PageNumber,
            page.PageSize,
            page.TotalCount
        );
    }
}
