namespace TodoTasks.Api.Features.Tasks.ListTodoTasks;

using Interlocutor.Abstractions.Core;
using Interlocutor.AspNetCore;
using TodoTasks.Api.Infrastructure;

/// <summary>
/// Maps <c>GET /todo-tasks?onlyIncomplete=...</c> - authenticated, and scoped to the caller's own tasks.
/// </summary>
public static class ListTodoTasksEndpoint
{
    /// <summary>Registers the list-tasks endpoint on <paramref name="app"/>.</summary>
    /// <param name="app">The application to add the endpoint to.</param>
    /// <returns><paramref name="app"/>, for chaining alongside this project's other <c>Map*Endpoint</c> calls.</returns>
    public static WebApplication MapListTodoTasksEndpoint(this WebApplication app)
    {
        app.MapGet(
                "/todo-tasks",
                async (
                    ISender sender,
                    IRequestContextAccessor contextAccessor,
                    CancellationToken ct,
                    bool onlyIncomplete = false
                ) =>
                {
                    // See CreateTodoTaskEndpoint's own remarks on RequireUserId().
                    var ownerId = contextAccessor.RequireUserId();

                    var result = await sender
                        .SendAsync(new ListTodoTasksQuery(onlyIncomplete, ownerId), ct)
                        .ConfigureAwait(false);
                    return result.ToMinimalApiResult();
                }
            )
            .RequireAuthorization();

        return app;
    }
}
