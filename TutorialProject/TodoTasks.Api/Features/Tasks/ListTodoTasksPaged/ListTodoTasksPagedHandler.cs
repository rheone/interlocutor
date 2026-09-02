namespace TodoTasks.Api.Features.Tasks.ListTodoTasksPaged;

using Interlocutor.Abstractions.Queries;
using Interlocutor.CommonKernel;
using Interlocutor.CommonKernel.ResultTypes;
using Interlocutor.Persistence.Abstractions;
using TodoTasks.Api.Domain;

/// <summary>Handles <see cref="ListTodoTasksPagedQuery"/> via <see cref="IDomainRepository{TEntity,TKey}.GetPagedAsync"/>.</summary>
public sealed class ListTodoTasksPagedHandler(IDomainRepository<TodoTask, int> tasks)
    : IQueryHandler<ListTodoTasksPagedQuery, Outcome<PagedTodoTasksDto, Failure>>
{
    /// <inheritdoc/>
    public async Task<Outcome<PagedTodoTasksDto, Failure>> Handle(ListTodoTasksPagedQuery query, CancellationToken ct)
    {
        var page = await tasks
            .GetPagedAsync(
                query.Page,
                query.PageSize,
                filter: t => t.OwnerId == query.OwnerId,
                sort: [new SortCriterion<TodoTask>(t => t.CreatedAtUtc, SortDirection.Descending)],
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
