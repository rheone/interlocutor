namespace TodoTasks.Api.Features.Tasks.ClearCompletedTodoTasks;

using Interlocutor.Abstractions.Core;
using Interlocutor.AspNetCore;
using Interlocutor.CommonKernel;

/// <summary>
/// Maps <c>POST /todo-tasks/clear-completed</c>. Unlike every other endpoint in this project, this
/// one has to state which outcome type it wants back explicitly
/// (<c>sender.Send&lt;OperationOutcome&gt;(...)</c>) - it can't be inferred from a bare
/// <see cref="Interlocutor.Abstractions.Commands.ICommand"/> the way it can from
/// <see cref="Interlocutor.Abstractions.Commands.ICommand{TResponse}"/>'s fixed response type.
/// </summary>
public static class ClearCompletedTodoTasksEndpoint
{
    /// <summary>Registers the clear-completed endpoint on <paramref name="app"/>.</summary>
    /// <param name="app">The application to add the endpoint to.</param>
    /// <returns><paramref name="app"/>, for chaining alongside this project's other <c>Map*Endpoint</c> calls.</returns>
    public static WebApplication MapClearCompletedTodoTasksEndpoint(this WebApplication app)
    {
        app.MapPost(
                "/todo-tasks/clear-completed",
                async (ISender sender, IRequestContextAccessor contextAccessor, CancellationToken ct) =>
                {
                    var ownerId =
                        contextAccessor.Context?.UserId
                        ?? throw new InvalidOperationException("This endpoint requires an authenticated caller.");

                    var result = await sender.Send<OperationOutcome>(new ClearCompletedTodoTasksCommand(ownerId), ct);
                    return result.ToMinimalApiResult();
                }
            )
            .RequireAuthorization();

        return app;
    }
}
