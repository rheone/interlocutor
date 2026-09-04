namespace TodoTasks.Api.Features.Tasks;

using Interlocutor.Abstractions.Core;
using Interlocutor.AspNetCore;
using Interlocutor.CommonKernel;
using Interlocutor.CommonKernel.ResultTypes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TodoTasks.Api.Domain;
using TodoTasks.Api.Features.Tasks.ClearCompletedTodoTasks;
using TodoTasks.Api.Features.Tasks.CompleteTodoTask;
using TodoTasks.Api.Features.Tasks.CreateTodoTask;
using TodoTasks.Api.Features.Tasks.DeleteTodoTask;
using TodoTasks.Api.Features.Tasks.GetTodoTaskById;
using TodoTasks.Api.Features.Tasks.ListTodoTasks;
using TodoTasks.Api.Features.Tasks.ReopenTodoTask;
using TodoTasks.Api.Features.Tasks.UpdateTodoTask;
using TodoTasks.Api.Infrastructure;

/// <summary>
/// The MVC-controller twin of this domain's minimal-API surface (<c>Features/Tasks/*/Map*Endpoint.cs</c>,
/// mapped at the root, e.g. <c>/todo-tasks</c>) - the same commands/queries/handlers, dispatched the same
/// way, exposed under <c>/api/todo-tasks</c> instead. Interlocutor doesn't care which ASP.NET Core transport
/// calls <c>ISender.Send(...)</c>: every action below runs through the exact same registered
/// pipeline (Logging → Auditing → Authorization → Validation → UnitOfWork, from <c>Program.cs</c>'s
/// <c>UseRecommendedChain()</c>) as its minimal-API counterpart, which is why no pipeline behavior needed
/// to change - or even know - to support this second transport. See
/// <c>documentation/sample-app-walkthrough.md</c>'s "Transport conversion at the edge" section.
/// </summary>
/// <remarks>
/// Constructor injection of <see cref="ISender"/>/<see cref="IRequestContextAccessor"/> is the same
/// inversion-of-control shape every handler and minimal-API endpoint in this project already uses - this
/// controller depends only on Interlocutor's abstractions, never on a concrete handler, repository, or
/// container. Autofac resolves these constructor parameters because <c>Program.cs</c> registers
/// <c>AddControllersAsServices()</c>, routing controller activation through the same container as
/// everything else.
/// </remarks>
[ApiController]
[Route("api/todo-tasks")]
public sealed class TodoTasksController(ISender sender, IRequestContextAccessor contextAccessor) : ControllerBase
{
    /// <summary>Handles <c>POST /api/todo-tasks</c> - the controller twin of <c>CreateTodoTaskEndpoint</c>.</summary>
    /// <param name="request">The task to create.</param>
    /// <param name="ct">A token to observe while dispatching.</param>
    /// <returns>
    /// 200 with the created task via <see cref="ResultExtensions.ToActionResult{TSuccess,TCase2,TCase3}"/>
    /// (not 201 - <c>ToActionResult()</c>'s success case is always <see cref="OkObjectResult"/>, regardless
    /// of HTTP verb), or the mapped failure.
    /// </returns>
    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Create(CreateTodoTaskRequest request, CancellationToken ct)
    {
        // A missing authenticated caller here is a bug, not an outcome the caller can trigger - [Authorize]
        // above means this action is never reachable without one; see CreateTodoTaskEndpoint's identical
        // comment for why this throws rather than returning a Forbidden/Failure case.
        var ownerId = contextAccessor.RequireUserId();

        var result = await sender
            .Send(new CreateTodoTaskCommand(request.Title, request.Notes, ownerId), ct)
            .ConfigureAwait(false);
        return result.ToActionResult();
    }

    /// <summary>
    /// Handles <c>GET /api/todo-tasks/{id}</c> - the controller twin of <c>GetTodoTaskByIdEndpoint</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Deliberately does <em>not</em> call <see cref="ResultExtensions.ToActionResult{TSuccess,TFailure}"/>.
    /// Instead, this pattern-matches <see cref="Outcome{TSuccessResult,TFailureResult}.Value"/>'s
    /// two possible cases directly - a real, compiler-checked exhaustive switch over the union, rather than
    /// the convenience extension hiding it. This is the trade-off the extension exists to avoid repeating:
    /// manual matching gives this one action full control over its response shape (and would let it diverge
    /// from <c>DefaultHttpOutcomeStatusMapper</c>'s status-code choices, on purpose or by accident - here it
    /// doesn't), whereas <see cref="ResultExtensions.ToActionResult{TSuccess,TFailure}"/> centralizes that
    /// mapping once for every other action in this project, this controller's own <see cref="Update"/>
    /// included.
    /// </para>
    /// <para>
    /// Contrast with <c>UsersController.GetById</c>, which handles the identically-shaped
    /// <c>Outcome&lt;UserDto, NotFound&gt;</c> union via <c>ToActionResult()</c> instead - the two
    /// actions together show both styles applied to the same union shape.
    /// </para>
    /// </remarks>
    /// <param name="id">The task's identifier.</param>
    /// <param name="ct">A token to observe while dispatching.</param>
    /// <returns>200 with the task on success, or 404 if no task with that id exists.</returns>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var result = await sender.Send(new GetTodoTaskByIdQuery(id), ct).ConfigureAwait(false);
        return result.Value switch
        {
            TodoTaskDto dto => this.Ok(dto),
            NotFound notFound => this.NotFound(notFound.Message),
            _ => throw new NotImplementedException(),
        };
    }

    /// <summary>Handles <c>GET /api/todo-tasks?onlyIncomplete=...</c> - the controller twin of <c>ListTodoTasksEndpoint</c>.</summary>
    /// <param name="onlyIncomplete">When <see langword="true"/>, only tasks not yet completed are returned.</param>
    /// <param name="ct">A token to observe while dispatching.</param>
    /// <returns>200 with the matching tasks, or the mapped failure.</returns>
    [HttpGet]
    public async Task<IActionResult> List(bool onlyIncomplete, CancellationToken ct)
    {
        var result = await sender.Send(new ListTodoTasksQuery(onlyIncomplete), ct).ConfigureAwait(false);
        return result.ToActionResult();
    }

    /// <summary>Handles <c>PUT /api/todo-tasks/{id}</c> - the controller twin of <c>UpdateTodoTaskEndpoint</c>.</summary>
    /// <param name="id">The task to update.</param>
    /// <param name="request">The task's new title/notes.</param>
    /// <param name="ct">A token to observe while dispatching.</param>
    /// <returns>200 with the updated task, 404 if it doesn't exist, or the mapped failure (e.g. not the owner).</returns>
    [HttpPut("{id:int}")]
    [Authorize]
    public async Task<IActionResult> Update(int id, UpdateTodoTaskRequest request, CancellationToken ct)
    {
        var requestedBy = contextAccessor.RequireUserId();

        var result = await sender
            .Send(new UpdateTodoTaskCommand(id, request.Title, request.Notes, requestedBy), ct)
            .ConfigureAwait(false);
        return result.ToActionResult();
    }

    /// <summary>Handles <c>POST /api/todo-tasks/{id}/complete</c> - the controller twin of <c>CompleteTodoTaskEndpoint</c>.</summary>
    /// <param name="id">The task to complete.</param>
    /// <param name="ct">A token to observe while dispatching.</param>
    /// <returns>200 with the completed task, 404 if it doesn't exist, or 409 if already completed.</returns>
    [HttpPost("{id:int}/complete")]
    public async Task<IActionResult> Complete(int id, CancellationToken ct)
    {
        var result = await sender.Send(new CompleteTodoTaskCommand(id), ct).ConfigureAwait(false);
        return result.ToActionResult();
    }

    /// <summary>Handles <c>POST /api/todo-tasks/{id}/reopen</c> - the controller twin of <c>ReopenTodoTaskEndpoint</c>.</summary>
    /// <param name="id">The task to reopen.</param>
    /// <param name="ct">A token to observe while dispatching.</param>
    /// <returns>200 with the reopened task, 404 if it doesn't exist, or 409 if not currently completed.</returns>
    [HttpPost("{id:int}/reopen")]
    public async Task<IActionResult> Reopen(int id, CancellationToken ct)
    {
        var result = await sender.Send(new ReopenTodoTaskCommand(id), ct).ConfigureAwait(false);
        return result.ToActionResult();
    }

    /// <summary>
    /// Handles <c>DELETE /api/todo-tasks/{id}</c> - the controller twin of <c>DeleteTodoTaskEndpoint</c>.
    /// Note this action does <em>not</em> carry <c>[Authorize]</c> the way <see cref="Create"/> and
    /// <see cref="Update"/> do - <see cref="DeleteTodoTaskCommand"/>'s own <c>IRequiresPolicy</c> marker is
    /// what gates it, evaluated by <c>AuthorizationBehavior</c> inside the pipeline, not by ASP.NET Core's
    /// action-level authorization. Same split <c>DeleteTodoTaskEndpoint</c> documents for the minimal-API
    /// version of this same command.
    /// </summary>
    /// <param name="id">The task to delete.</param>
    /// <param name="ct">A token to observe while dispatching.</param>
    /// <returns>
    /// 200 with an empty <see cref="Success"/> body on success (not 204 - <see cref="DeleteTodoTaskCommand"/>
    /// returns <c>Outcome&lt;Success, NotFound, Forbidden&gt;</c>, whose success case
    /// <c>ToActionResult()</c> maps the same way as any other generic union success case), 404 if it
    /// doesn't exist, or 403 if the policy check fails.
    /// </returns>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var result = await sender.Send(new DeleteTodoTaskCommand(id), ct).ConfigureAwait(false);
        return result.ToActionResult();
    }

    /// <summary>
    /// Handles <c>POST /api/todo-tasks/clear-completed</c> - the controller twin of
    /// <c>ClearCompletedTodoTasksEndpoint</c>. Like that endpoint, this has to state which outcome type it
    /// wants back explicitly (<c>sender.Send&lt;Outcome&gt;(...)</c>) - it can't be inferred from a
    /// bare <see cref="Interlocutor.Abstractions.Commands.ICommand"/> the way it can from
    /// <see cref="Interlocutor.Abstractions.Commands.ICommand{TResponse}"/>'s fixed response type.
    /// </summary>
    /// <param name="ct">A token to observe while dispatching.</param>
    /// <returns>204 on success, or the mapped failure.</returns>
    [HttpPost("clear-completed")]
    [Authorize]
    public async Task<IActionResult> ClearCompleted(CancellationToken ct)
    {
        var ownerId = contextAccessor.RequireUserId();

        var result = await sender.Send<Outcome>(new ClearCompletedTodoTasksCommand(ownerId), ct).ConfigureAwait(false);
        return result.ToActionResult();
    }
}
