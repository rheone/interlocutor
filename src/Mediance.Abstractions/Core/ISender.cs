using Mediance.Abstractions.Commands;
using Mediance.Abstractions.Queries;
using Mediance.CommonKernel;

namespace Mediance.Abstractions.Core;

/// <summary>Send-only interface for request/response operations. Exposes <see cref="Send{TResponse}(IQuery{TResponse},CancellationToken)"/> for queries and commands.</summary>
public interface ISender
{
    /// <summary>Dispatches a query to its registered handler, returning the result through all pipeline behaviors.</summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task<TResponse> Send<TResponse>(IQuery<TResponse> query, CancellationToken ct = default);

    /// <summary>Dispatches a value-returning command to its registered handler, returning the result through all pipeline behaviors.</summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task<TResponse> Send<TResponse>(ICommand<TResponse> command, CancellationToken ct = default);

    /// <summary>Dispatches a void command to its registered handler, returning the result through all pipeline behaviors.</summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task<Result> Send(ICommand command, CancellationToken ct = default);
}
