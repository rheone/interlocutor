namespace TodoTasks.Api.Features.Tasks.SearchTodoTasks;

using global::NHibernate;
using global::NHibernate.Criterion;
using Interlocutor.Abstractions.Queries;
using Interlocutor.CommonKernel;
using Interlocutor.CommonKernel.ResultTypes;
using TodoTasks.Api.Domain;
// Microsoft.NET.Sdk.Web's implicit usings bring in Microsoft.AspNetCore.Http's own ISession (the HTTP
// session-state abstraction), which would otherwise collide with NHibernate's ISession below - see
// Program.cs's own alias for the same reason.
using ISession = global::NHibernate.ISession;

/// <summary>
/// Handles <see cref="SearchTodoTasksQuery"/> by querying NHibernate's <see cref="ISession"/> directly
/// via its QueryOver API, bypassing <c>IDomainRepository{TEntity,TKey}</c> entirely.
/// </summary>
/// <remarks>
/// Backlog ticket 14. <c>IDomainRepository{TEntity,TKey}</c> covers a filter expression, a sort, and
/// paging - a fixed method set that can't express an insensitive-like title search cleanly through a
/// LINQ <c>Contains</c> expression. QueryOver, NHibernate's fluent Criteria API, expresses that search
/// directly. This is still safe: the injected <see cref="Lazy{T}"/> resolves the exact same scoped
/// <see cref="ISession"/> every other repository and <c>IUnitOfWork</c> in this dispatch shares (see
/// Step 4 of <c>documentation/beginners-guide-todo-api.md</c>), so this handler still participates in
/// whatever transaction <c>UnitOfWorkBehavior</c> is managing - it never opens or holds a session of its
/// own. Never share the resolved <see cref="ISession"/> across concurrent threads or more than one open
/// transaction per connection - NHibernate's own concurrency contract for <see cref="ISession"/> - which
/// registering it scoped, one session per dispatch, satisfies by construction.
/// </remarks>
public sealed class SearchTodoTasksHandler(Lazy<ISession> session)
    : IQueryHandler<SearchTodoTasksQuery, OperationOutcome<IReadOnlyList<TodoTaskDto>, Failure>>
{
    /// <inheritdoc/>
    public async Task<OperationOutcome<IReadOnlyList<TodoTaskDto>, Failure>> Handle(
        SearchTodoTasksQuery query,
        CancellationToken ct
    )
    {
        var tasks = await session
            .Value.QueryOver<TodoTask>()
            .Where(t => t.OwnerId == query.OwnerId)
            .AndRestrictionOn(t => t.Title)
            .IsInsensitiveLike(query.TitleContains, MatchMode.Anywhere)
            .OrderBy(t => t.CreatedAtUtc)
            .Desc.ListAsync(ct);

        return tasks.Select(TodoTaskDto.FromEntity).ToList();
    }
}
