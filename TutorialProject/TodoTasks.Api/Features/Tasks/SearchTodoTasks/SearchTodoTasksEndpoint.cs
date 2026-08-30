namespace TodoTasks.Api.Features.Tasks.SearchTodoTasks;

using Interlocutor.Abstractions.Core;
using Interlocutor.AspNetCore;

/// <summary>Maps <c>GET /todo-tasks/search</c> - authenticated, scoped to the caller's own tasks.</summary>
public static class SearchTodoTasksEndpoint
{
    /// <summary>Registers the search-tasks endpoint on <paramref name="app"/>.</summary>
    /// <param name="app">The application to add the endpoint to.</param>
    /// <returns><paramref name="app"/>, for chaining alongside this project's other <c>Map*Endpoint</c> calls.</returns>
    public static WebApplication MapSearchTodoTasksEndpoint(this WebApplication app)
    {
        app.MapGet(
                "/todo-tasks/search",
                async (string titleContains, ISender sender, IRequestContextAccessor contextAccessor, CancellationToken ct) =>
                {
                    var ownerId =
                        contextAccessor.Context?.UserId
                        ?? throw new InvalidOperationException("This endpoint requires an authenticated caller.");

                    var result = await sender.Send(new SearchTodoTasksQuery(ownerId, titleContains), ct).ConfigureAwait(false);
                    return result.ToMinimalApiResult();
                }
            )
            .RequireAuthorization();

        return app;
    }
}
