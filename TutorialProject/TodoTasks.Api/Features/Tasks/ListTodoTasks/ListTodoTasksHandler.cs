namespace TodoTasks.Api.Features.Tasks.ListTodoTasks;

using Interlocutor.Abstractions.Queries;
using Interlocutor.CommonKernel;
using Interlocutor.CommonKernel.ResultTypes;
using Interlocutor.Persistence.Abstractions;
using TodoTasks.Api.Domain;

/// <summary>
/// Handles <see cref="ListTodoTasksQuery"/> - unpaged, sorted newest-first, scoped to
/// <see cref="ListTodoTasksQuery.OwnerId"/> so one caller never sees another caller's tasks.
/// </summary>
/// <remarks>
/// <para>
/// Returning a large list unpaged is a starting point, not a permanent design - once the list can grow
/// without bound, backlog ticket 12 (paging) is where that gets addressed, via
/// <c>IDomainRepository{TEntity,TKey}.GetPagedAsync</c> instead of <see cref="IDomainRepository{TEntity,TKey}.GetManyAsync"/>.
/// </para>
/// <para>
/// Sorted client-side after materializing, rather than via
/// <see cref="IDomainRepository{TEntity,TKey}.GetManyAsync"/>'s own <c>sort</c> parameter - EF Core's
/// SQLite provider cannot translate an <c>ORDER BY</c> over a
/// <see cref="DateTimeOffset"/> column into SQL (SQLite has no native <see cref="DateTimeOffset"/> type),
/// so pushing <see cref="TodoTask.CreatedAtUtc"/> ordering down to the database throws at query time
/// against the real store even though it works against any mocked <c>IDomainRepository{TEntity,TKey}</c>.
/// </para>
/// </remarks>
public sealed class ListTodoTasksHandler(IDomainRepository<TodoTask, int> tasks)
    : IQueryHandler<ListTodoTasksQuery, Outcome<IReadOnlyList<TodoTaskDto>, Failure>>
{
    /// <inheritdoc/>
    public async Task<Outcome<IReadOnlyList<TodoTaskDto>, Failure>> Handle(ListTodoTasksQuery query, CancellationToken ct)
    {
        var matches = await tasks
            .GetManyAsync(
                filter: query.OnlyIncomplete
                    ? t => t.OwnerId == query.OwnerId && !t.IsCompleted
                    : t => t.OwnerId == query.OwnerId,
                cancellationToken: ct
            )
            .ConfigureAwait(false);

        return matches.OrderByDescending(t => t.CreatedAtUtc).Select(TodoTaskDto.FromEntity).ToList();
    }
}
