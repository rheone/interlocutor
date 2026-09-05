namespace TodoTasks.Api.Features.Tasks.ReopenTodoTask;

using Interlocutor.Abstractions.Core;
using Interlocutor.AspNetCore;
using TodoTasks.Api.Infrastructure;

/// <summary>Maps <c>POST /todo-tasks/{id}/reopen</c>.</summary>
public static class ReopenTodoTaskEndpoint
{
    /// <summary>Registers the reopen-task endpoint on <paramref name="app"/>.</summary>
    /// <param name="app">The application to add the endpoint to.</param>
    /// <returns><paramref name="app"/>, for chaining alongside this project's other <c>Map*Endpoint</c> calls.</returns>
    public static WebApplication MapReopenTodoTaskEndpoint(this WebApplication app)
    {
        app.MapPost(
                "/todo-tasks/{id:int}/reopen",
                async (int id, ISender sender, IRequestContextAccessor contextAccessor, CancellationToken ct) =>
                {
                    // See CreateTodoTaskEndpoint's own remarks on RequireUserId().
                    var requestedBy = contextAccessor.RequireUserId();

                    var result = await sender.SendAsync(new ReopenTodoTaskCommand(id, requestedBy), ct).ConfigureAwait(false);
                    return result.ToMinimalApiResult();
                }
            )
            .RequireAuthorization();

        return app;
    }
}
