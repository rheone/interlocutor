namespace Mediance.Abstractions.Queries;

/// <summary>
/// Defines a handler that processes a CQRS query of a specified type and returns a result.
/// </summary>
/// <typeparam name="TQuery">The type of the query this handler can process.</typeparam>
/// <typeparam name="TResponse">The type of data the query produces.</typeparam>
public interface IQueryHandler<in TQuery, TResponse>
    where TQuery : IQuery<TResponse>
{
    /// <summary>
    /// Handles the specified query and returns a result that represents success or failure.
    /// </summary>
    /// <param name="query">The query to handle.</param>
    /// <param name="ct">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains the
    /// query's response — which may itself be a union type distinguishing success from failure.
    /// </returns>
    Task<TResponse> Handle(TQuery query, CancellationToken ct);
}
