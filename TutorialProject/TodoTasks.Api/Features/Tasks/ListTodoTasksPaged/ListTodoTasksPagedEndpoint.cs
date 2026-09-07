namespace TodoTasks.Api.Features.Tasks.ListTodoTasksPaged;

using Interlocutor.Abstractions.Core;
using Interlocutor.AspNetCore;
using TodoTasks.Api.Infrastructure;

/// <summary>Maps <c>GET /todo-tasks/paged</c> - authenticated, scoped to the caller's own tasks.</summary>
public static class ListTodoTasksPagedEndpoint
{
    /// <summary>Registers the paged-list-tasks endpoint on <paramref name="app"/>.</summary>
    /// <param name="app">The application to add the endpoint to.</param>
    /// <returns><paramref name="app"/>, for chaining alongside this project's other <c>Map*Endpoint</c> calls.</returns>
    public static WebApplication MapListTodoTasksPagedEndpoint(this WebApplication app)
    {
        app.MapGet(
                "/todo-tasks/paged",
                async (int page, int pageSize, ISender sender, IRequestContextAccessor contextAccessor, CancellationToken ct) =>
                {
                    // See CreateTodoTaskEndpoint's own remarks on RequireUserId().
                    var ownerId = contextAccessor.RequireUserId();

                    var result = await sender
                        .Send(new ListTodoTasksPagedQuery(ownerId, page, pageSize), ct)
                        .ConfigureAwait(false);
                    return result.ToMinimalApiResult();
                }
            )
            .RequireAuthorization();

        return app;
    }
}
