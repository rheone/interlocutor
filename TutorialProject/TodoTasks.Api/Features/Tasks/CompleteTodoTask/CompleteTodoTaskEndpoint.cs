namespace TodoTasks.Api.Features.Tasks.CompleteTodoTask;

using Interlocutor.Abstractions.Core;
using Interlocutor.AspNetCore;
using TodoTasks.Api.Infrastructure;

/// <summary>Maps <c>POST /todo-tasks/{id}/complete</c>.</summary>
public static class CompleteTodoTaskEndpoint
{
    /// <summary>Registers the complete-task endpoint on <paramref name="app"/>.</summary>
    /// <param name="app">The application to add the endpoint to.</param>
    /// <returns><paramref name="app"/>, for chaining alongside this project's other <c>Map*Endpoint</c> calls.</returns>
    public static WebApplication MapCompleteTodoTaskEndpoint(this WebApplication app)
    {
        app.MapPost(
                "/todo-tasks/{id:int}/complete",
                async (int id, ISender sender, IRequestContextAccessor contextAccessor, CancellationToken ct) =>
                {
                    // See CreateTodoTaskEndpoint's own remarks on RequireUserId() - a missing authenticated
                    // caller here is a bug, not an outcome the caller can trigger, since RequireAuthorization()
                    // below means this endpoint is never reachable without one.
                    var requestedBy = contextAccessor.RequireUserId();

                    var result = await sender.Send(new CompleteTodoTaskCommand(id, requestedBy), ct).ConfigureAwait(false);
                    return result.ToMinimalApiResult();
                }
            )
            .RequireAuthorization();

        return app;
    }
}
