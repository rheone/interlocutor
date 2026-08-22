namespace Interlocutor.Sample.Handlers.Shared;

using Interlocutor.Abstractions.Queries;
using Interlocutor.CommonKernel;
using Interlocutor.CommonKernel.ResultTypes;

/// <summary>
/// Looks up any entity type by its primary key. Generic over <typeparamref name="TEntity"/> rather than
/// written once per entity - see <see cref="GetByIdHandler{TEntity}"/>, the single open generic handler
/// that serves every closed instantiation of this query (<see cref="Tags.TagItem"/>,
/// <see cref="Todos.TodoItem"/>) with no per-entity handler code.
/// </summary>
/// <typeparam name="TEntity">The entity type to look up.</typeparam>
/// <param name="Id">The entity's primary key.</param>
public sealed record GetByIdQuery<TEntity>(int Id) : IQuery<OperationOutcome<TEntity, NotFound>>
    where TEntity : class;
