namespace TodoTasks.Api.Features.Tasks.GetTodoTaskById;

using Interlocutor.Abstractions.Core;
using Interlocutor.AspNetCore;
using TodoTasks.Api.Infrastructure;

/// <summary>Maps <c>GET /todo-tasks/{id}</c> - authenticated, and scoped to the caller's own tasks.</summary>
public static class GetTodoTaskByIdEndpoint
{
    /// <summary>Registers the get-task-by-id endpoint on <paramref name="app"/>.</summary>
    /// <param name="app">The application to add the endpoint to.</param>
    /// <returns><paramref name="app"/>, for chaining alongside this project's other <c>Map*Endpoint</c> calls.</returns>
    public static WebApplication MapGetTodoTaskByIdEndpoint(this WebApplication app)
    {
        app.MapGet(
                "/todo-tasks/{id:int}",
                async (int id, ISender sender, IRequestContextAccessor contextAccessor, CancellationToken ct) =>
                {
                    // See CreateTodoTaskEndpoint's own remarks on RequireUserId() - a missing authenticated
                    // caller here is a bug, not an outcome the caller can trigger, since RequireAuthorization()
                    // below means this endpoint is never reachable without one.
                    var ownerId = contextAccessor.RequireUserId();

                    var result = await sender.Send(new GetTodoTaskByIdQuery(id, ownerId), ct).ConfigureAwait(false);

                    return result.ToMinimalApiResult();
                }
            )
            .RequireAuthorization();

        return app;
    }
}
