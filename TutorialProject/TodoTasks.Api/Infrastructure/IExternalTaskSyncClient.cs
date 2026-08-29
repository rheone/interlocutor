namespace TodoTasks.Api.Infrastructure;

/// <summary>
/// A small stand-in for an external system that can independently mark a task complete outside this
/// API - illustrative only, never a real integration this project ships. Exists purely so
/// <c>Features/Tasks/SyncAwareCompleteTodoTask/SyncAwareCompleteTodoTaskHandler</c> has something real to
/// check before deciding whether to commit or cancel its own staged write.
/// </summary>
public interface IExternalTaskSyncClient
{
    /// <summary>
    /// Reports whether the given task was already completed by the external system this stands in for,
    /// independently of this API.
    /// </summary>
    /// <param name="taskId">The task's identifier.</param>
    /// <param name="cancellationToken">A token to observe while waiting for the check to complete.</param>
    /// <returns><see langword="true"/> if the external system already completed this task; otherwise <see langword="false"/>.</returns>
    Task<bool> WasAlreadyCompletedElsewhereAsync(int taskId, CancellationToken cancellationToken = default);
}
