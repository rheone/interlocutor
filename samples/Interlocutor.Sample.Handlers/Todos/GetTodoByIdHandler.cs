namespace Interlocutor.Sample.Handlers.Todos;

using Interlocutor.Abstractions.Queries;
using Interlocutor.CommonKernel;
using Interlocutor.CommonKernel.ResultTypes;
using Microsoft.EntityFrameworkCore;

/// <summary>The dedicated handler for <see cref="GetTodoByIdQuery"/> - a plain query handler, no transaction.</summary>
/// <param name="db">The sample's database context.</param>
public sealed class GetTodoByIdHandler(SampleDbContext db)
    : IQueryHandler<GetTodoByIdQuery, OperationOutcome<TodoItem, NotFound>>
{
    /// <inheritdoc/>
    public async Task<OperationOutcome<TodoItem, NotFound>> Handle(GetTodoByIdQuery query, CancellationToken ct)
    {
        var todo = await db.TodoItems.FindAsync([query.Id], ct);
        return todo is null ? new NotFound($"To-do {query.Id} not found") : todo;
    }
}
