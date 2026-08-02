namespace Mediance.Abstractions.Queries;

/// <summary>
/// Marks a type as a CQRS query and defines the type of data produced when the query is handled.
/// </summary>
/// <typeparam name="TResponse">The type of data the query produces.</typeparam>
/// <remarks>
/// <para>
/// This is a marker interface with no members. It is used to constrain
/// <see cref="IQueryHandler{TQuery,TResponse}"/> so the Mediance pipeline can
/// dispatch queries to their registered handlers.
/// </para>
/// </remarks>
public interface IQuery<TResponse>;
