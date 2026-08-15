namespace Interlocutor.Abstractions.Queries;

using Interlocutor.CommonKernel;

/// <summary>
/// Marks a type as a CQRS query and defines the type of data produced when the query is handled.
/// </summary>
/// <typeparam name="TResponse">
/// The <see cref="IOperationOutcome"/>-shaped union the query produces. Queries never return a bare DTO —
/// the response must be (or be wrapped in) an outcome union so callers exhaustively handle every case.
/// </typeparam>
/// <remarks>
/// <para>
/// This is a marker interface with no members. It is used to constrain
/// <see cref="IQueryHandler{TQuery,TResponse}"/> so the Interlocutor pipeline can
/// dispatch queries to their registered handlers.
/// </para>
/// </remarks>
public interface IQuery<TResponse>
    where TResponse : IOperationOutcome;
