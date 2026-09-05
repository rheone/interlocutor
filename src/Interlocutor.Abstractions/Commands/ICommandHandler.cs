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
/// instead, which returns an <see cref="CommonKernel.ResultTypes.IOutcome"/> union.
/// </para>
/// <para>
/// Handlers are registered and discovered by the mediator via DI assembly scanning. Register each handler in
/// your DI container (typically via <c>AddInterlocutor</c>). The mediator expects exactly one handler per
/// <c>&lt;TCommand, TResponse&gt;</c> pair.
/// </para>
/// </remarks>
public interface ICommandHandler<in TCommand, TResponse>
    where TCommand : ICommand<TResponse>
    where TResponse : IOutcome
{
    /// <summary>
    /// Handles the specified command and returns a result containing the response or a failure. Superseded by
    /// <see cref="HandleAsync"/> - implement that member in new handlers instead.
    /// </summary>
    /// <param name="command">The command to handle.</param>
    /// <param name="ct">Cancellation token to cancel the operation.</param>
    /// <returns>The response data, which may itself be a union type distinguishing success from failure.</returns>
    /// <remarks>
    /// Carries no <c>Async</c> suffix despite returning a <see cref="Task{TResult}"/>, unlike the rest of this
    /// codebase's awaitable-returning members - <see cref="HandleAsync"/> carries the suffix instead. Existing
    /// implementations of this member keep working - the default <see cref="HandleAsync"/> forwards to it - but
    /// new handlers should implement <see cref="HandleAsync"/> instead.
    /// </remarks>
    Task<TResponse> Handle(TCommand command, CancellationToken ct);

    /// <summary>
    /// The preferred, <c>Async</c>-suffixed member going forward: handles the specified command and returns a
    /// result containing the response or a failure.
    /// </summary>
    /// <param name="command">The command to handle.</param>
    /// <param name="ct">Cancellation token to cancel the operation.</param>
    /// <returns>The response data, which may itself be a union type distinguishing success from failure.</returns>
    /// <remarks>
    /// The default implementation forwards to <see cref="Handle"/> so every existing implementer keeps
    /// compiling and behaving identically until it migrates to overriding this member instead.
    /// </remarks>
    Task<TResponse> HandleAsync(TCommand command, CancellationToken ct) => this.Handle(command, ct);
}
