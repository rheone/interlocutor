namespace Interlocutor.Abstractions.Core;

using Interlocutor.Abstractions.Commands;
using Interlocutor.Abstractions.Queries;
using Interlocutor.CommonKernel;

/// <summary>Send-only interface for request/response operations. Exposes Send overloads for queries and commands.</summary>
/// <remarks>
/// <para>
/// Use <see cref="ISender"/> for synchronous request/response workflows (queries reading data, commands writing data
/// and returning a result). Every Send call runs through the full pipeline behavior chain
/// before returning a typed response to the caller.
/// </para>
/// <para>
/// Three generic overloads handle different response patterns: queries returning typed data, commands returning
/// typed data, and commands returning an operation outcome. A fourth, non-generic overload
/// (<see cref="Send(object, CancellationToken)"/>) dispatches a request whose type is known only at runtime.
/// </para>
/// </remarks>
public interface ISender
{
    /// <summary>Dispatches a query to its registered handler, returning the result through all pipeline behaviors.</summary>
    /// <typeparam name="TResponse">The query response type.</typeparam>
    /// <param name="query">The query to dispatch.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A <see cref="Task{TResult}"/> representing the asynchronous operation.</returns>
    Task<TResponse> Send<TResponse>(IQuery<TResponse> query, CancellationToken ct = default)
        where TResponse : IOperationOutcome;

    /// <summary>Dispatches a value-returning command to its registered handler, returning the result through all pipeline behaviors.</summary>
    /// <typeparam name="TResponse">The command response type.</typeparam>
    /// <param name="command">The command to dispatch.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A <see cref="Task{TResult}"/> representing the asynchronous operation.</returns>
    Task<TResponse> Send<TResponse>(ICommand<TResponse> command, CancellationToken ct = default)
        where TResponse : IOperationOutcome;

    /// <summary>Dispatches a void command to its registered handler, returning the outcome through all pipeline behaviors.</summary>
    /// <typeparam name="TResult">The <see cref="Interlocutor.CommonKernel.IOperationOutcome"/>-shaped outcome type to resolve a handler for.</typeparam>
    /// <param name="command">The command to dispatch.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A <see cref="Task{TResult}"/> representing the asynchronous operation.</returns>
    Task<TResult> Send<TResult>(ICommand command, CancellationToken ct = default)
        where TResult : IOperationOutcome;

    /// <summary>
    /// Dispatches a request whose static type is not known at the call site — a query, a value-returning
    /// command, or a unit command deserialized off a message — to its registered handler, returning the
    /// result through all pipeline behaviors as the library's base outcome interface.
    /// </summary>
    /// <param name="request">
    /// The request to dispatch. Its runtime type must implement <c>IQuery&lt;TResponse&gt;</c>,
    /// <c>ICommand&lt;TResponse&gt;</c>, or <c>ICommand</c>.
    /// </param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>
    /// A <see cref="Task{TResult}"/> whose result is the handler's response as an
    /// <see cref="Interlocutor.CommonKernel.IOperationOutcome"/>. Callers can check
    /// <c>IsSuccess</c>-shaped members or pattern-match the union's case value (via
    /// <see cref="System.Runtime.CompilerServices.IUnion"/>) without knowing the concrete outcome type
    /// statically.
    /// </returns>
    /// <remarks>
    /// <para>
    /// Intended for infrastructure callers — an outbox, an inbox, or a queue consumer — that deserialize a
    /// message into an <see cref="object"/> with no static request type. The generic <c>Send</c> overloads
    /// infer their response type from the argument's static type, which a deserialized message doesn't have;
    /// this overload determines the response type reflectively from the request's runtime type instead.
    /// </para>
    /// <para>
    /// This overload is implemented reflectively for now — it locates the closed <c>IQuery{TResponse}</c> or
    /// <c>ICommand{TResponse}</c> interface (or, for a unit command, the registered
    /// <c>IVoidCommandHandler{TCommand, TResult}</c>) on the request's runtime type and invokes the matching
    /// generic <c>Send</c> overload via reflection, so pipeline composition is not duplicated. A future
    /// generator-backed implementation will replace the reflection with a compiled type switch without
    /// changing this contract.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="request"/> is <see langword="null"/>.</exception>
    /// <exception cref="Interlocutor.Abstractions.Core.UnsupportedRequestTypeException">
    /// <paramref name="request"/>'s runtime type does not implement <c>IQuery&lt;TResponse&gt;</c>,
    /// <c>ICommand&lt;TResponse&gt;</c>, or <c>ICommand</c> — it is not a request at all.
    /// </exception>
    /// <exception cref="Interlocutor.Abstractions.Core.HandlerNotRegisteredException">
    /// <paramref name="request"/>'s runtime type is a recognized request shape, but no handler for it is
    /// registered in the service provider.
    /// </exception>
    Task<IOperationOutcome> Send(object request, CancellationToken ct = default);
}
