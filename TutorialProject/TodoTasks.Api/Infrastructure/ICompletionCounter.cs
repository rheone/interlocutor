namespace TodoTasks.Api.Infrastructure;

/// <summary>
/// Tracks how many <see cref="Domain.TodoTask"/>s each caller has completed - a small in-memory
/// stand-in so <see cref="TodoTasks.Api.Features.Tasks.CompleteTodoTask.TodoTaskCompletionCounterHandler"/>
/// has something real to do, per <c>documentation/beginners-guide-todo-api.md</c> Step 8.
/// </summary>
public interface ICompletionCounter
{
    /// <summary>Increments the completion count for one caller.</summary>
    /// <param name="ownerId">The caller whose count to increment.</param>
    void Increment(string ownerId);

    /// <summary>
    /// Gets the current completion count for one caller. Not part of the guide's own interface - added
    /// here so this counter is actually inspectable (by tests, or a future endpoint) rather than a
    /// write-only black box.
    /// </summary>
    /// <param name="ownerId">The caller whose count to read.</param>
    /// <returns>The number of tasks that caller has completed, or <c>0</c> if none.</returns>
    int CountFor(string ownerId);
}
