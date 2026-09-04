namespace TodoTasks.Api.Features.Tasks.UpdateTodoTask;

/// <summary>The shape of an update-task HTTP request body - deliberately narrower than <see cref="UpdateTodoTaskCommand"/>.</summary>
/// <param name="Title">The task's new title.</param>
/// <param name="Notes">The task's new notes.</param>
public sealed record UpdateTodoTaskRequest(string Title, string? Notes);
