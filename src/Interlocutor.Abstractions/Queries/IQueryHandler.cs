namespace Interlocutor.Abstractions.Queries;

using Interlocutor.CommonKernel.ResultTypes;

/// <summary>
/// Defines a handler that processes a CQRS query of a specified type and returns a result.
/// </summary>
/// <typeparam name="TQuery">The type of the query this handler can process.</typeparam>
/// <typeparam name="TResponse">The type of data the query produces.</typeparam>
/// <remarks>
/// <para>
/// Implement this interface to define the business logic for a specific query type. The mediator discovers and
/// resolves implementations from DI, so register each handler in your DI container (typically via
/// <c>AddInterlocutor</c> assembly scanning). Multiple handlers for the same query type are not allowed - the
/// mediator expects exactly one registered handler per <c>&lt;TQuery, TResponse&gt;</c> pair.
/// </para>
/// <para>
/// Handlers run through the pipeline behavior chain, which means cross-cutting concerns (logging, validation,
/// authorization, transactions) can intercept the query before or after your handler logic. Keep handler logic
/// focused on the business operation; defer infrastructure concerns to behaviors.
/// </para>
/// </remarks>
public interface IQueryHandler<in TQuery, TResponse>
    where TQuery : IQuery<TResponse>
    where TResponse : IOutcome
{
    /// <summary>
    /// Handles the specified query and returns a result that represents success or failure. Superseded by
    /// <see cref="HandleAsync"/> - implement that member in new handlers instead.
    /// </summary>
    /// <param name="query">The query to handle.</param>
    /// <param name="ct">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains the
    /// query's response - which may itself be a union type distinguishing success from failure.
    /// </returns>
    /// <remarks>
    /// Carries no <c>Async</c> suffix despite returning a <see cref="Task{TResult}"/>, unlike the rest of this
    /// codebase's awaitable-returning members - <see cref="HandleAsync"/> carries the suffix instead. Existing
    /// implementations of this member keep working - the default <see cref="HandleAsync"/> forwards to it - but
    /// new handlers should implement <see cref="HandleAsync"/> instead.
    /// </remarks>
    Task<TResponse> Handle(TQuery query, CancellationToken ct);

    /// <summary>
    /// The preferred, <c>Async</c>-suffixed member going forward: handles the specified query and returns a
    /// result that represents success or failure.
    /// </summary>
    /// <param name="query">The query to handle.</param>
    /// <param name="ct">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains the
    /// query's response - which may itself be a union type distinguishing success from failure.
    /// </returns>
    /// <remarks>
    /// The default implementation forwards to <see cref="Handle"/> so every existing implementer keeps
    /// compiling and behaving identically until it migrates to overriding this member instead.
    /// </remarks>
    Task<TResponse> HandleAsync(TQuery query, CancellationToken ct) => this.Handle(query, ct);
}
