namespace TodoTasks.Api.Features.Tasks.SearchTodoTasks;

using Interlocutor.Abstractions.Queries;
using Interlocutor.CommonKernel;
using Interlocutor.CommonKernel.ResultTypes;
using Microsoft.EntityFrameworkCore;
using TodoTasks.Api.Domain;
using TodoTasks.Api.Persistence;

/// <summary>
/// Handles <see cref="SearchTodoTasksQuery"/> by querying <see cref="TodoTasksDbContext"/> directly,
/// bypassing <c>IDomainRepository{TEntity,TKey}</c> entirely.
/// </summary>
/// <remarks>
/// <c>IDomainRepository{TEntity,TKey}</c> covers a filter expression, a sort, and paging - a fixed
/// method set that can't express a case-insensitive substring title search cleanly through it. This
/// handler expresses that search directly against the <see cref="DbSet{TEntity}"/> instead, via
/// <see cref="DbFunctionsExtensions.Like(DbFunctions, string, string)"/> - SQLite's own <c>LIKE</c>
/// operator is case-insensitive for ASCII by default (unlike a plain LINQ
/// <see cref="string.Contains(string)"/>, which this provider translates to the byte-comparing
/// <c>instr()</c> function instead, which is case-sensitive). This is still safe: the injected
/// <see cref="Lazy{T}"/> resolves the exact same scoped
/// <see cref="TodoTasksDbContext"/> every other repository and <c>IUnitOfWork</c> in this dispatch
/// shares (see <c>Program.cs</c>'s own registration of <c>Lazy&lt;TodoTasksDbContext&gt;</c>), so this
/// handler still participates in whatever transaction the unit-of-work behavior is managing - it never
/// opens or holds a <see cref="TodoTasksDbContext"/> of its own.
/// </remarks>
public sealed class SearchTodoTasksHandler(Lazy<TodoTasksDbContext> context)
    : IQueryHandler<SearchTodoTasksQuery, Outcome<IReadOnlyList<TodoTaskDto>, Failure>>
{
    /// <inheritdoc/>
    public async Task<Outcome<IReadOnlyList<TodoTaskDto>, Failure>> Handle(SearchTodoTasksQuery query, CancellationToken ct)
    {
        var pattern = $"%{query.TitleContains}%";

        // Sorted client-side after materializing, not via an ORDER BY pushed into the query - the same
        // reason ListTodoTasksHandler sorts client-side: EF Core's SQLite provider cannot translate an
        // ORDER BY over a DateTimeOffset column into SQL (SQLite has no native DateTimeOffset type).
        var tasks = await context
            .Value.Tasks.Where(t => t.OwnerId == query.OwnerId && EF.Functions.Like(t.Title, pattern))
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return tasks.OrderByDescending(t => t.CreatedAtUtc).Select(TodoTaskDto.FromEntity).ToList();
    }
}
