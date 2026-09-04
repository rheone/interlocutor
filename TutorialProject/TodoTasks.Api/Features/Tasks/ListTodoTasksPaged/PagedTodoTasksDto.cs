namespace TodoTasks.Api.Features.Tasks.ListTodoTasksPaged;

using TodoTasks.Api.Domain;

/// <summary>
/// One page of the caller's own <see cref="TodoTask"/>s, together with the paging metadata needed to
/// render pagination controls.
/// </summary>
/// <param name="Items">The tasks belonging to <paramref name="Page"/>.</param>
/// <param name="Page">The one-based page number this page represents.</param>
/// <param name="PageSize">The maximum number of items per page that was requested.</param>
/// <param name="TotalCount">The total number of matching tasks across every page, ignoring paging.</param>
/// <remarks>
/// <c>Interlocutor.Persistence.Abstractions.Page{T}</c> is repository-return-shape vocabulary, not an
/// <c>Outcome</c> success-case type - see that type's own remarks - so <see cref="ListTodoTasksPagedHandler"/>
/// maps it into this project-owned type instead of returning it unwrapped, the same way
/// <see cref="TodoTaskDto"/> stands in front of <see cref="TodoTask"/> everywhere else in this project.
/// </remarks>
public sealed record PagedTodoTasksDto(IReadOnlyList<TodoTaskDto> Items, int Page, int PageSize, int TotalCount);
