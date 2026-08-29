namespace Interlocutor.Abstractions.Queries;

using Interlocutor.CommonKernel.ResultTypes;

/// <summary>
/// Marks a type as a CQRS query and defines the type of data produced when the query is handled.
/// </summary>
/// <typeparam name="TResponse">
/// The <see cref="IOperationOutcome"/>-shaped union the query produces. Queries never return a bare DTO -
/// the response must be (or be wrapped in) an outcome union so callers exhaustively handle every case.
/// </typeparam>
/// <remarks>
/// <para>
/// This is a marker interface with no members. It is used to constrain
/// <see cref="IQueryHandler{TQuery,TResponse}"/> so the Interlocutor pipeline can
/// dispatch queries to their registered handlers.
/// </para>
/// <para>
/// <typeparamref name="TResponse"/> is a deliberate dispatch marker, not something any member of this
/// interface needs directly - it lets the mediator's generic <c>Send&lt;TResponse&gt;</c> overload select
/// the matching handler and pipeline behaviors at compile time. S2326 flags it as "unused" because no
/// member signature here mentions it, but removing it would erase the compile-time link between a query
/// and its response type entirely.
/// </para>
/// </remarks>
#pragma warning disable S2326
public interface IQuery<TResponse>
    where TResponse : IOperationOutcome;
#pragma warning restore S2326
