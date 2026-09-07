namespace TodoTasks.Api.Features.Tasks.ListTodoTasksPaged;

using Interlocutor.Abstractions.Queries;
using Interlocutor.CommonKernel;
using Interlocutor.CommonKernel.ResultTypes;

/// <summary>Lists a single page of the authenticated caller's own <see cref="Domain.TodoTask"/>s, newest first.</summary>
/// <param name="OwnerId">
/// The authenticated caller's id - filled in by the endpoint from <c>IRequestContextAccessor</c>, never
/// trusted from the request itself. Scopes the result to this caller's own tasks only.
/// </param>
/// <param name="Page">The one-based page number to return.</param>
/// <param name="PageSize">The maximum number of tasks to return for this page.</param>
/// <remarks>
/// The bounded alternative to <see cref="ListTodoTasks.ListTodoTasksQuery"/> once an unpaged list stops
/// being a safe default - see <see cref="ListTodoTasksPagedHandler"/>.
/// </remarks>
public sealed record ListTodoTasksPagedQuery(string OwnerId, int Page, int PageSize)
    : IQuery<Outcome<PagedTodoTasksDto, Failure>>;
