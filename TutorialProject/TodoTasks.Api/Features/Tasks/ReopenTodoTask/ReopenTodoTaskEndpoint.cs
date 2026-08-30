namespace TodoTasks.Api.Features.Tasks.ReopenTodoTask;

using Interlocutor.Abstractions.Core;
using Interlocutor.AspNetCore;

/// <summary>Maps <c>POST /todo-tasks/{id}/reopen</c>. Unauthenticated, matching the guide's own running example - see <c>CompleteTodoTaskEndpoint</c>'s equivalent note.</summary>
public static class ReopenTodoTaskEndpoint
{
    /// <summary>Registers the reopen-task endpoint on <paramref name="app"/>.</summary>
    /// <param name="app">The application to add the endpoint to.</param>
    /// <returns><paramref name="app"/>, for chaining alongside this project's other <c>Map*Endpoint</c> calls.</returns>
    public static WebApplication MapReopenTodoTaskEndpoint(this WebApplication app)
    {
        app.MapPost(
            "/todo-tasks/{id:int}/reopen",
            async (int id, ISender sender, CancellationToken ct) =>
            {
                var result = await sender.Send(new ReopenTodoTaskCommand(id), ct).ConfigureAwait(false);
                return result.ToMinimalApiResult();
            }
        );

        return app;
    }
}
