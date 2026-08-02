namespace Mediance.Abstractions.Commands;

using Mediance.CommonKernel;

/// <summary>
/// Handles a command that produces no data payload — only an outcome.
/// </summary>
/// <typeparam name="TCommand">The command type that this handler processes.</typeparam>
/// <typeparam name="TResult">
/// The <see cref="IOperationOutcome"/>-shaped union describing the outcome. Chosen by the handler/call site
/// — may be any public union type in <c>Mediance.CommonKernel</c> (<see cref="OperationOutcome"/>,
/// <see cref="OperationOutcome{TSuccessResult, TFailureResult}"/>, etc.).
/// </typeparam>
public interface IVoidCommandHandler<in TCommand, TResult>
    where TCommand : ICommand
    where TResult : IOperationOutcome
{
    /// <summary>
    /// Handles the specified command and returns the outcome.
    /// </summary>
    /// <param name="command">The command to handle.</param>
    /// <param name="ct">Cancellation token to cancel the operation.</param>
    /// <returns>The outcome of handling the command.</returns>
    Task<TResult> Handle(TCommand command, CancellationToken ct);
}
