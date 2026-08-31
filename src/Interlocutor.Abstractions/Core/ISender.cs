namespace Interlocutor.Abstractions.Core;

using System.Diagnostics.CodeAnalysis;
using Interlocutor.Abstractions.Commands;
using Interlocutor.Abstractions.Queries;
using Interlocutor.CommonKernel.ResultTypes;

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
        where TResponse : IOutcome;

    /// <summary>Dispatches a value-returning command to its registered handler, returning the result through all pipeline behaviors.</summary>
    /// <typeparam name="TResponse">The command response type.</typeparam>
    /// <param name="command">The command to dispatch.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A <see cref="Task{TResult}"/> representing the asynchronous operation.</returns>
    Task<TResponse> Send<TResponse>(ICommand<TResponse> command, CancellationToken ct = default)
        where TResponse : IOutcome;

    /// <summary>Dispatches a void command to its registered handler, returning the outcome through all pipeline behaviors.</summary>
    /// <typeparam name="TResult">The <see cref="CommonKernel.ResultTypes.IOutcome"/>-shaped outcome type to resolve a handler for.</typeparam>
    /// <param name="command">The command to dispatch.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A <see cref="Task{TResult}"/> representing the asynchronous operation.</returns>
    Task<TResult> Send<TResult>(ICommand command, CancellationToken ct = default)
        where TResult : IOutcome;

    /// <summary>
    /// Dispatches a request whose static type is not known at the call site - a query, a value-returning
    /// command, or a unit command deserialized off a message - to its registered handler, returning the
    /// result through all pipeline behaviors as the library's base outcome interface.
    /// </summary>
    /// <param name="request">
    /// The request to dispatch. Its runtime type must implement <c>IQuery&lt;TResponse&gt;</c>,
    /// <c>ICommand&lt;TResponse&gt;</c>, or <c>ICommand</c>.
    /// </param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>
    /// A <see cref="Task{TResult}"/> whose result is the handler's response as an
    /// <see cref="CommonKernel.ResultTypes.IOutcome"/>. Callers can check
    /// <c>IsSuccess</c>-shaped members or pattern-match the union's case value (via
    /// <see cref="System.Runtime.CompilerServices.IUnion"/>) without knowing the concrete outcome type
    /// statically.
    /// </returns>
    /// <remarks>
    /// <para>
    /// Intended for infrastructure callers - an outbox, an inbox, or a queue consumer - that deserialize a
    /// message into an <see cref="object"/> with no static request type. The generic <c>Send</c> overloads
    /// infer their response type from the argument's static type, which a deserialized message doesn't have;
    /// this overload determines the response type reflectively from the request's runtime type instead.
    /// </para>
    /// <para>
    /// When <c>Interlocutor.Generator</c> ran over the compilation and its output was passed to
    /// <c>InterlocutorBuilder.AddGeneratedRegistrations</c>, this overload dispatches through a compile-time
    /// generated type switch (<see cref="IGeneratedRequestDispatcher"/>) - no reflection runs on that path.
    /// Otherwise it falls back to locating the closed <c>IQuery{TResponse}</c> or <c>ICommand{TResponse}</c>
    /// interface (or, for a unit command, the registered <c>IVoidCommandHandler{TCommand, TResult}</c>) on the
    /// request's runtime type and invoking the matching generic <c>Send</c> overload via reflection - the
    /// explicitly opted-in runtime-scanning fallback. Either way this method's contract - exceptions included
    /// - is identical to the caller.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="request"/> is <see langword="null"/>.</exception>
    /// <exception cref="Interlocutor.Abstractions.Core.UnsupportedRequestTypeException">
    /// <paramref name="request"/>'s runtime type does not implement <c>IQuery&lt;TResponse&gt;</c>,
    /// <c>ICommand&lt;TResponse&gt;</c>, or <c>ICommand</c> - it is not a request at all.
    /// </exception>
    /// <exception cref="Interlocutor.Abstractions.Core.HandlerNotRegisteredException">
    /// <paramref name="request"/>'s runtime type is a recognized request shape, but no handler for it is
    /// registered in the service provider.
    /// </exception>
    [RequiresUnreferencedCode(
        "May run the runtime-typed dispatch fallback when no generated dispatcher is registered - see "
            + "documentation/trim-and-aot-support.md."
    )]
    [RequiresDynamicCode(
        "May run the runtime-typed dispatch fallback when no generated dispatcher is registered - see "
            + "documentation/trim-and-aot-support.md."
    )]
    Task<IOutcome> Send(object request, CancellationToken ct = default);
}
