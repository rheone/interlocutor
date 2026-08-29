namespace TodoTasks.Api.Features.Tasks.CompleteTodoTask;

using Interlocutor.Abstractions.Core;
using Interlocutor.AspNetCore;

/// <summary>
/// Maps <c>POST /todo-tasks/{id}/complete</c>. Unauthenticated, matching the guide's own running
/// example exactly - <see cref="CompleteTodoTaskHandler"/> makes no ownership check, so there's
/// nothing here for authentication to gate.
/// </summary>
public static class CompleteTodoTaskEndpoint
{
    /// <summary>Registers the complete-task endpoint on <paramref name="app"/>.</summary>
    /// <param name="app">The application to add the endpoint to.</param>
    /// <returns><paramref name="app"/>, for chaining alongside this project's other <c>Map*Endpoint</c> calls.</returns>
    public static WebApplication MapCompleteTodoTaskEndpoint(this WebApplication app)
    {
        app.MapPost(
            "/todo-tasks/{id:int}/complete",
            async (int id, ISender sender, CancellationToken ct) =>
            {
                var result = await sender.Send(new CompleteTodoTaskCommand(id), ct);
                return result.ToMinimalApiResult();
            }
        );

        return app;
    }
}
