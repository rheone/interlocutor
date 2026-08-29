namespace TodoTasks.Api.Features.Tasks.SyncAwareCompleteTodoTask;

using Interlocutor.Abstractions.Commands;
using Interlocutor.CommonKernel;
using Interlocutor.CommonKernel.ResultTypes;
using TodoTasks.Api.Domain;

/// <summary>
/// Completes a <see cref="TodoTask"/>, deferring to an external system if it already completed the same
/// task independently.
/// </summary>
/// <param name="Id">The task's identifier.</param>
/// <remarks>
/// Backlog ticket 16 - demonstrates <c>IUnitOfWork.Cancel</c>, the rare escape hatch for a handler that
/// stages a write and then discovers mid-flight it shouldn't commit, without that being a
/// <see cref="Failure"/>-shaped outcome. See <see cref="SyncAwareCompleteTodoTaskHandler"/>'s own remarks
/// for why this is a distinct command from <c>CompleteTodoTask.CompleteTodoTaskCommand</c> rather than a
/// second handler for it, and why it is deliberately not wired into <c>Program.cs</c> as the project's
/// real completion path.
/// </remarks>
public sealed record SyncAwareCompleteTodoTaskCommand(int Id) : ICommand<OperationOutcome<TodoTaskDto, NotFound>>;
