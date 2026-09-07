namespace TodoTasks.Api.Infrastructure;

using Interlocutor.Abstractions.Core;
using Interlocutor.Abstractions.Pipeline;
using Interlocutor.Persistence.Abstractions;
using TodoTasks.Api.Domain;
using TodoTasks.Api.Features.Tasks.DeleteTodoTask;

/// <summary>
/// A stand-in for a real policy system - <c>AuthorizationBehavior</c> only ever asks "is this policy
/// name satisfied for this request," so any engine can sit behind this one method. In a production app
/// this would typically delegate to ASP.NET Core's <c>IAuthorizationService</c> instead.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="OwnsTaskPolicyName"/> is deliberately resource-specific, unlike a typical
/// <c>IRequiresPolicy</c> policy (a coarse "can this caller attempt this kind of command at all" check
/// with no resource in play yet - see <see cref="Interlocutor.Abstractions.Markers.IRequiresPolicy"/>'s
/// own remarks). This evaluator loads the <see cref="TodoTask"/> named by the request and compares its
/// <see cref="TodoTask.OwnerId"/> against the authenticated caller, purely to demonstrate that
/// <c>AuthorizationBehavior</c> *can* express an entity-level check when a team wants that decision made
/// before the pipeline ever reaches a handler - contrast this with
/// <c>Features/Tasks/UpdateTodoTask/UpdateTodoTaskHandler</c> and
/// <c>Features/Tasks/CompleteTodoTask</c>/<c>ReopenTodoTask</c>'s handlers, which make the identical
/// ownership decision the other, more common way: inside <c>ResourceCommandHandler</c>'s own
/// <c>Authorize</c> hook, after the entity is already loaded for the handler's own use.
/// </para>
/// <para>
/// A task id that doesn't exist is deliberately treated as *authorized* here, not denied: this
/// evaluator's job is only "does the caller own this task, if it exists" - whether the task exists at
/// all is left to <see cref="DeleteTodoTaskHandler"/> to answer, so a missing task still produces
/// <see cref="Interlocutor.CommonKernel.ResultTypes.NotFound"/> rather than being masked as
/// <see cref="Interlocutor.CommonKernel.ResultTypes.Forbidden"/> by a policy check that never had an
/// entity to compare ownership against.
/// </para>
/// </remarks>
public sealed class TodoTasksPolicyEvaluator(IDomainRepository<TodoTask, int> tasks, IRequestContextAccessor contextAccessor)
    : IPolicyEvaluator
{
    /// <summary>The policy name <see cref="DeleteTodoTaskCommand"/> declares via <c>IRequiresPolicy</c>.</summary>
    public const string OwnsTaskPolicyName = "owns-task";

    /// <inheritdoc/>
    public Task<bool> EvaluateAsync(string policyName, object request, CancellationToken ct)
    {
        if (policyName != OwnsTaskPolicyName || request is not DeleteTodoTaskCommand command)
        {
            return Task.FromResult(false);
        }

        if (contextAccessor.Context?.UserId is not { } callerId)
        {
            return Task.FromResult(false);
        }

        // No such task: not this evaluator's call to make - let the request through so
        // DeleteTodoTaskHandler produces NotFound instead of this policy masking it as Forbidden.
        var task = tasks.GetById(command.Id);
        var authorized = task is null || task.OwnerId == callerId;
        return Task.FromResult(authorized);
    }
}
