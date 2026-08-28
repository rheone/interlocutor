namespace Interlocutor.Abstractions.Commands;

using Interlocutor.CommonKernel.ResultTypes;

/// <summary>
/// Handles a command that returns a typed response.
/// </summary>
/// <typeparam name="TCommand">The command type that this handler processes.</typeparam>
/// <typeparam name="TResponse">The type of the response data produced by the handler.</typeparam>
/// <remarks>
/// <para>
/// Use this handler for commands that mutate state and return domain data (e.g., <c>CreateUserCommand → UserDto</c>).
/// For commands that produce only success/failure outcomes (not domain data), use <see cref="IVoidCommandHandler{TCommand, TResult}"/>
/// instead, which returns an <see cref="CommonKernel.ResultTypes.IOperationOutcome"/> union.
/// </para>
/// <para>
/// Handlers are registered and discovered by the mediator via DI assembly scanning. Register each handler in
/// your DI container (typically via <c>AddInterlocutor</c>). The mediator expects exactly one handler per
/// <c>&lt;TCommand, TResponse&gt;</c> pair.
/// </para>
/// </remarks>
public interface ICommandHandler<in TCommand, TResponse>
    where TCommand : ICommand<TResponse>
    where TResponse : IOperationOutcome
{
    /// <summary>
    /// Handles the specified command and returns a result containing the response or a failure.
    /// </summary>
    /// <param name="command">The command to handle.</param>
    /// <param name="ct">Cancellation token to cancel the operation.</param>
    /// <returns>The response data, which may itself be a union type distinguishing success from failure.</returns>
    Task<TResponse> Handle(TCommand command, CancellationToken ct);
}
