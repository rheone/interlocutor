namespace Interlocutor.IntegrationTests;

using Interlocutor.Abstractions.Commands;
using Interlocutor.Abstractions.Markers;
using Interlocutor.CommonKernel;
using Interlocutor.CommonKernel.ResultTypes;

/// <summary>
/// The tracer-bullet command this project dispatches end to end: creates one <see cref="Widget"/>, opting into
/// every marker the canonical chain (Logging → Auditing → Authorization → Validation → Transaction) needs to
/// actually run each of its behaviors, rather than skip one via unmet generic constraints.
/// </summary>
/// <param name="Name">The widget's name - validated non-empty by <see cref="CreateWidgetCommandValidator"/>.</param>
public sealed record CreateWidgetCommand(string Name)
    : ICommand<Outcome<Success, ValidationErrors, Forbidden>>,
        IAuditableRequest,
        IRequiresPolicyRequest
{
    /// <inheritdoc/>
    public string ActionName => "create_widget";

    /// <inheritdoc/>
    /// <remarks>
    /// Evaluated against the try-added <c>DefaultPolicyEvaluator</c> (nothing overrides <c>IPolicyEvaluator</c>
    /// in this project's tests), which grants every policy unconditionally - so this name is never actually
    /// checked against anything, only exercised for real by <c>AuthorizationBehavior&lt;TRequest,TResponse&gt;</c>
    /// running in the chain and delegating to it.
    /// </remarks>
    public string PolicyName => "AlwaysAllow";
}
