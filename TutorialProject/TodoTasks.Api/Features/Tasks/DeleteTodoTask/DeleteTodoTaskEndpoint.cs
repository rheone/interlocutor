namespace TodoTasks.Api.Features.Tasks.DeleteTodoTask;

using Interlocutor.Abstractions.Core;
using Interlocutor.AspNetCore;

/// <summary>Maps <c>DELETE /todo-tasks/{id}</c>.</summary>
/// <remarks>
/// Unlike the other authenticated write endpoints in this project, this one doesn't read the caller's
/// id off <see cref="IRequestContextAccessor"/> itself - <see cref="DeleteTodoTaskCommand"/>'s
/// ownership check happens entirely inside the pipeline, via its <c>IRequiresPolicy</c> marker and
/// <see cref="Infrastructure.TodoTasksPolicyEvaluator"/>. <c>RequireAuthorization()</c> below still
/// matters: it's what rejects a request with no authenticated caller at all before dispatch, the same
/// 401 every other write endpoint in this project returns for that case.
/// </remarks>
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
                    var result = await sender.SendAsync(new DeleteTodoTaskCommand(id), ct).ConfigureAwait(false);
                    return result.ToMinimalApiResult();
                }
            )
            .RequireAuthorization();

        return app;
    }
}
