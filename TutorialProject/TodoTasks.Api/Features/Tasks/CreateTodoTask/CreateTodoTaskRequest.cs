namespace TodoTasks.Api.Features.Tasks.CreateTodoTask;

/// <summary>
/// The shape of a create-task HTTP request body - deliberately narrower than
/// <see cref="CreateTodoTaskCommand"/> (no <c>OwnerId</c> - that comes from the authenticated caller,
/// never the request body).
/// </summary>
/// <param name="Title">The new task's title.</param>
/// <param name="Notes">Optional free-text notes.</param>
public sealed record CreateTodoTaskRequest(string Title, string? Notes);
