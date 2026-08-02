namespace Mediance.Abstractions.Core;

using Mediance.Abstractions.Commands;
using Mediance.Abstractions.Queries;
using Mediance.CommonKernel;

/// <summary>Send-only interface for request/response operations. Exposes Send overloads for queries and commands.</summary>
/// <remarks>
/// <para>
/// Use <see cref="ISender"/> for synchronous request/response workflows (queries reading data, commands writing data
/// and returning a result). Every Send call runs through the full pipeline behavior chain
/// before returning a typed response to the caller.
/// </para>
/// <para>
/// Three overloads handle different response patterns: queries returning typed data, commands returning typed data, and commands returning an operation outcome.
/// </para>
/// </remarks>
public interface ISender
{
    /// <summary>Dispatches a query to its registered handler, returning the result through all pipeline behaviors.</summary>
    /// <typeparam name="TResponse">The query response type.</typeparam>
    /// <param name="query">The query to dispatch.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A <see cref="Task{TResult}"/> representing the asynchronous operation.</returns>
    Task<TResponse> Send<TResponse>(IQuery<TResponse> query, CancellationToken ct = default);

    /// <summary>Dispatches a value-returning command to its registered handler, returning the result through all pipeline behaviors.</summary>
    /// <typeparam name="TResponse">The command response type.</typeparam>
    /// <param name="command">The command to dispatch.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A <see cref="Task{TResult}"/> representing the asynchronous operation.</returns>
    Task<TResponse> Send<TResponse>(ICommand<TResponse> command, CancellationToken ct = default);

    /// <summary>Dispatches a void command to its registered handler, returning the outcome through all pipeline behaviors.</summary>
    /// <typeparam name="TResult">The <see cref="Mediance.CommonKernel.IOperationOutcome"/>-shaped outcome type to resolve a handler for.</typeparam>
    /// <param name="command">The command to dispatch.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A <see cref="Task{TResult}"/> representing the asynchronous operation.</returns>
    Task<TResult> Send<TResult>(ICommand command, CancellationToken ct = default)
        where TResult : IOperationOutcome;
}
