namespace TodoTasks.Api.Features.Tasks.DeleteTodoTask;

using Interlocutor.Abstractions.Core;
using Interlocutor.AspNetCore;

/// <summary>
/// Maps <c>DELETE /todo-tasks/{id}</c>. Note this endpoint does <em>not</em> call
/// <c>RequireAuthorization()</c> the way the authenticated write endpoints elsewhere in this project
/// do - <see cref="DeleteTodoTaskCommand"/>'s own <c>IRequiresPolicy</c> marker is what gates it,
/// evaluated by <c>AuthorizationBehavior</c> inside the pipeline, not by ASP.NET Core's endpoint-level
/// authorization.
/// </summary>
public static class DeleteTodoTaskEndpoint
{
    /// <summary>Registers the delete-task endpoint on <paramref name="app"/>.</summary>
    /// <param name="app">The application to add the endpoint to.</param>
    /// <returns><paramref name="app"/>, for chaining alongside this project's other <c>Map*Endpoint</c> calls.</returns>
    public static WebApplication MapDeleteTodoTaskEndpoint(this WebApplication app)
    {
        app.MapDelete(
            "/todo-tasks/{id:int}",
            async (int id, ISender sender, CancellationToken ct) =>
            {
                var result = await sender.Send(new DeleteTodoTaskCommand(id), ct);
                return result.ToMinimalApiResult();
            }
        );

        return app;
    }
}
