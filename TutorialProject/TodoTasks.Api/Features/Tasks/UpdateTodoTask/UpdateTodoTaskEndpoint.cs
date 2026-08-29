namespace TodoTasks.Api.Features.Tasks.UpdateTodoTask;

using Interlocutor.Abstractions.Core;
using Interlocutor.AspNetCore;

/// <summary>Maps <c>PUT /todo-tasks/{id}</c>.</summary>
public static class UpdateTodoTaskEndpoint
{
    /// <summary>Registers the update-task endpoint on <paramref name="app"/>.</summary>
    /// <param name="app">The application to add the endpoint to.</param>
    /// <returns><paramref name="app"/>, for chaining alongside this project's other <c>Map*Endpoint</c> calls.</returns>
    public static WebApplication MapUpdateTodoTaskEndpoint(this WebApplication app)
    {
        app.MapPut(
                "/todo-tasks/{id:int}",
                async (
                    int id,
                    UpdateTodoTaskRequest request,
                    ISender sender,
                    IRequestContextAccessor contextAccessor,
                    CancellationToken ct
                ) =>
                {
                    var requestedBy =
                        contextAccessor.Context?.UserId
                        ?? throw new InvalidOperationException("This endpoint requires an authenticated caller.");

                    var result = await sender.Send(
                        new UpdateTodoTaskCommand(id, request.Title, request.Notes, requestedBy),
                        ct
                    );
                    return result.ToMinimalApiResult();
                }
            )
            .RequireAuthorization();

        return app;
    }
}
