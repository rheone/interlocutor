namespace Interlocutor.Sample.Handlers.Todos;

using Interlocutor.Abstractions.Pipeline;
using Interlocutor.CommonKernel;
using Interlocutor.CommonKernel.ResultTypes;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Declines a <see cref="CreateTodoCommand"/> whose title already exists, before the handler - and the
/// transaction it opens - ever runs. The sample's short-circuiting pipeline stage: cheaper than letting a
/// doomed request reach the handler, and a case <see cref="CreateTodoHandler"/> itself never has to consider.
/// </summary>
/// <param name="db">The sample's database context.</param>
public sealed class DuplicateTitleGuardStage(SampleDbContext db)
    : PipelineStageBase<CreateTodoCommand, OperationOutcome<TodoItem, ValidationErrors, Conflict>>
{
    /// <inheritdoc/>
    public override async Task<StageResult<OperationOutcome<TodoItem, ValidationErrors, Conflict>>> OnBeforeAsync(
        CreateTodoCommand request,
        CancellationToken ct
    )
    {
        var title = request.Title.Trim();
        var exists = await db.TodoItems.AnyAsync(t => t.Title == title, ct);
        return exists
            ? StageResult<OperationOutcome<TodoItem, ValidationErrors, Conflict>>.ShortCircuit(
                new Conflict($"A to-do titled '{title}' already exists.", "duplicate-title")
            )
            : StageResult<OperationOutcome<TodoItem, ValidationErrors, Conflict>>.Continue;
    }
}
