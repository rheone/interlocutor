namespace TodoTasks.Api.Features.Tasks.SearchTodoTasks;

using Interlocutor.Abstractions.Queries;
using Interlocutor.CommonKernel;
using Interlocutor.CommonKernel.ResultTypes;
using TodoTasks.Api.Domain;

/// <summary>
/// Searches the caller's own <see cref="TodoTask"/>s for a case-insensitive title match.
/// </summary>
/// <param name="OwnerId">The authenticated caller's id - only their own tasks are searched.</param>
/// <param name="TitleContains">The substring to match against <see cref="TodoTask.Title"/>, case-insensitively.</param>
/// <remarks>
/// Answered by <see cref="SearchTodoTasksHandler"/>, which drops to EF Core's <see cref="TodoTasks.Api.Persistence.TodoTasksDbContext"/>
/// directly instead of going through <c>IDomainRepository{TEntity,TKey}</c>. See that handler's own
/// remarks for why.
/// </remarks>
public sealed record SearchTodoTasksQuery(string OwnerId, string TitleContains)
    : IQuery<Outcome<IReadOnlyList<TodoTaskDto>, Failure>>;
