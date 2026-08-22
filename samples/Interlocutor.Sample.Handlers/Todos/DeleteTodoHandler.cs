namespace Interlocutor.Sample.Handlers.Todos;

using Interlocutor.Abstractions.Commands;
using Interlocutor.CommonKernel;
using Interlocutor.CommonKernel.ResultTypes;
using Microsoft.EntityFrameworkCore;

/// <summary>The handler for <see cref="DeleteTodoCommand"/> - deletes if present, reports if not.</summary>
/// <param name="db">The sample's database context.</param>
public sealed class DeleteTodoHandler(SampleDbContext db)
    : IVoidCommandHandler<DeleteTodoCommand, OperationOutcome<Success, NotFound>>
{
    /// <inheritdoc/>
    public async Task<OperationOutcome<Success, NotFound>> Handle(DeleteTodoCommand command, CancellationToken ct)
    {
        var todo = await db.TodoItems.FindAsync([command.Id], ct);
        if (todo is null)
        {
            return new NotFound($"To-do {command.Id} not found");
        }

        db.TodoItems.Remove(todo);
        return default(Success);
    }
}
