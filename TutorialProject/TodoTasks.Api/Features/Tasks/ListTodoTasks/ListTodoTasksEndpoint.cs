namespace TodoTasks.Api.Features.Tasks.ListTodoTasks;

using Interlocutor.Abstractions.Core;
using Interlocutor.AspNetCore;

/// <summary>Maps <c>GET /todo-tasks?onlyIncomplete=...</c>. Unauthenticated - a plain read, matching the guide's own running example.</summary>
public static class ListTodoTasksEndpoint
{
    /// <summary>Registers the list-tasks endpoint on <paramref name="app"/>.</summary>
    /// <param name="app">The application to add the endpoint to.</param>
    /// <returns><paramref name="app"/>, for chaining alongside this project's other <c>Map*Endpoint</c> calls.</returns>
    public static WebApplication MapListTodoTasksEndpoint(this WebApplication app)
    {
        app.MapGet(
            "/todo-tasks",
            async (bool onlyIncomplete, ISender sender, CancellationToken ct) =>
            {
                var result = await sender.Send(new ListTodoTasksQuery(onlyIncomplete), ct);
                return result.ToMinimalApiResult();
            }
        );

        return app;
    }
}
