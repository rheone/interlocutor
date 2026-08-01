using Mediance.CommonKernel.ResultTypes;

namespace Mediance.Abstractions.Commands;

/// <summary>
/// Handles a command that returns a typed response.
/// </summary>
/// <typeparam name="TCommand">The command type that this handler processes.</typeparam>
/// <typeparam name="TResponse">The type of the response data produced by the handler.</typeparam>
public interface ICommandHandler<in TCommand, TResponse>
    where TCommand : ICommand<TResponse>
{
    /// <summary>
    /// Handles the specified command and returns a result containing the response or a failure.
    /// </summary>
    /// <param name="command">The command to handle.</param>
    /// <param name="ct">Cancellation token to cancel the operation.</param>
    /// <returns>A <see cref="Result{T}"/> containing either the response data or a failure reason.</returns>
    Task<Result<TResponse>> Handle(TCommand command, CancellationToken ct);
}

/// <summary>
/// Handles a command that produces no response data.
/// </summary>
/// <typeparam name="TCommand">The command type that this handler processes.</typeparam>
public interface ICommandHandler<in TCommand>
    where TCommand : ICommand
{
    /// <summary>
    /// Handles the specified command and returns a success or failure result.
    /// </summary>
    /// <param name="command">The command to handle.</param>
    /// <param name="ct">Cancellation token to cancel the operation.</param>
    /// <returns>A <see cref="Result"/> indicating success or containing a failure reason.</returns>
    Task<Result> Handle(TCommand command, CancellationToken ct);
}
