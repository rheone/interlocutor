namespace TodoTasks.Api.Features.Tasks.GetTodoTaskById;

using Interlocutor.Abstractions.Core;
using Interlocutor.AspNetCore;

/// <summary>Maps <c>GET /todo-tasks/{id}</c>. Unauthenticated - a plain read, matching the guide's own running example.</summary>
public static class GetTodoTaskByIdEndpoint
{
    /// <summary>Registers the get-task-by-id endpoint on <paramref name="app"/>.</summary>
    /// <param name="app">The application to add the endpoint to.</param>
    /// <returns><paramref name="app"/>, for chaining alongside this project's other <c>Map*Endpoint</c> calls.</returns>
    public static WebApplication MapGetTodoTaskByIdEndpoint(this WebApplication app)
    {
        app.MapGet(
            "/todo-tasks/{id:int}",
            async (int id, ISender sender, CancellationToken ct) =>
            {
                var result = await sender.Send(new GetTodoTaskByIdQuery(id), ct);
                return result.ToMinimalApiResult();
            }
        );

        return app;
    }
}
