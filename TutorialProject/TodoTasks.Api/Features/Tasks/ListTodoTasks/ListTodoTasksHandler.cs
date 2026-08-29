namespace TodoTasks.Api.Features.Tasks.ListTodoTasks;

using Interlocutor.Abstractions.Queries;
using Interlocutor.CommonKernel;
using Interlocutor.CommonKernel.ResultTypes;
using Interlocutor.Persistence.Abstractions;
using TodoTasks.Api.Domain;

/// <summary>Handles <see cref="ListTodoTasksQuery"/> - unpaged, sorted newest-first.</summary>
/// <remarks>
/// Returning a large list unpaged is a starting point, not a permanent design - once the list can
/// grow without bound, backlog ticket 15 (paging) is where that gets addressed, via
/// <c>IDomainRepository{TEntity,TKey}.GetPagedAsync</c> instead of <see cref="IDomainRepository{TEntity,TKey}.GetManyAsync"/>.
/// </remarks>
public sealed class ListTodoTasksHandler(IDomainRepository<TodoTask, int> tasks)
    : IQueryHandler<ListTodoTasksQuery, OperationOutcome<IReadOnlyList<TodoTaskDto>, Failure>>
{
    /// <inheritdoc/>
    public async Task<OperationOutcome<IReadOnlyList<TodoTaskDto>, Failure>> Handle(
        ListTodoTasksQuery query,
        CancellationToken ct
    )
    {
        var matches = await tasks.GetManyAsync(
            filter: query.OnlyIncomplete ? t => !t.IsCompleted : null,
            sort: [new SortCriterion<TodoTask>(t => t.CreatedAtUtc, SortDirection.Descending)],
            cancellationToken: ct
        );

        return matches.Select(TodoTaskDto.FromEntity).ToList();
    }
}
