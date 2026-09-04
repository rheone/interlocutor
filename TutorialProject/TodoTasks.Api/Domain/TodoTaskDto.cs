namespace TodoTasks.Api.Domain;

/// <summary>
/// The shape of a <see cref="TodoTask"/> returned to callers - deliberately narrower than the entity
/// itself, so the API's response shape never accidentally changes just because the persistence model
/// does. See <see cref="UserDto"/> for the same pattern applied to <see cref="User"/>.
/// </summary>
/// <param name="Id">The task's identifier.</param>
/// <param name="Title">The task's title.</param>
/// <param name="Notes">The task's optional notes.</param>
/// <param name="IsCompleted">Whether the task has been completed.</param>
/// <param name="CreatedAtUtc">When the task was created.</param>
public sealed record TodoTaskDto(int Id, string Title, string? Notes, bool IsCompleted, DateTimeOffset CreatedAtUtc)
{
    /// <summary>Projects a <see cref="TodoTask"/> entity to its response shape.</summary>
    /// <param name="task">The entity to project.</param>
    /// <returns>The equivalent <see cref="TodoTaskDto"/>.</returns>
    public static TodoTaskDto FromEntity(TodoTask task) =>
        new(task.Id, task.Title, task.Notes, task.IsCompleted, task.CreatedAtUtc);
}
