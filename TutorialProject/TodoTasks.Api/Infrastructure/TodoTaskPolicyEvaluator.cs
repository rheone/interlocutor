namespace TodoTasks.Api.Infrastructure;

using Interlocutor.Abstractions.Core;
using Interlocutor.Abstractions.Pipeline;

/// <summary>
/// A stand-in for a real policy system - <c>AuthorizationBehavior</c> only ever asks "is this policy
/// name satisfied for this request," so any engine can sit behind this one method. In a production
/// app this would typically delegate to ASP.NET Core's <c>IAuthorizationService</c> instead.
/// </summary>
public sealed class TodoTaskPolicyEvaluator(IRequestContextAccessor contextAccessor) : IPolicyEvaluator
{
    /// <inheritdoc/>
    public Task<bool> EvaluateAsync(string policyName, object request, CancellationToken ct) =>
        Task.FromResult(
            policyName switch
            {
                "DeleteTodoTask" => contextAccessor.Context?.UserId is not null,
                _ => false,
            }
        );
}
