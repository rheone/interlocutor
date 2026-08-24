namespace Interlocutor.Sample.Handlers.Shared;

using System.Diagnostics.CodeAnalysis;
using Interlocutor.Abstractions.Queries;
using Interlocutor.CommonKernel;
using Interlocutor.CommonKernel.ResultTypes;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// The single handler behind every closed instantiation of <see cref="GetByIdQuery{TEntity}"/> - an open
/// generic handler, discovered and dispatched at compile time exactly like a closed one, that reads any
/// entity type out of <see cref="SampleDbContext"/> by primary key without per-entity handler code.
/// </summary>
/// <remarks>
/// Interlocutor.Generator closes an open generic handler over a request type only when it finds a fully
/// closed usage of that instantiation (for example <c>new GetByIdQuery&lt;TodoItem&gt;(...)</c>) in the
/// <em>same compilation</em> as the handler itself - it doesn't scan a referencing application's syntax
/// trees for usages. <see cref="GetByIdQueries"/>, in this same assembly, is what supplies those usages for
/// every entity type this handler serves; a caller in a different assembly (the web application, say)
/// dispatches through those typed entry points rather than constructing <see cref="GetByIdQuery{TEntity}"/>
/// directly.
/// </remarks>
/// <typeparam name="TEntity">The entity type this instantiation looks up.</typeparam>
/// <param name="db">The sample's database context.</param>
public sealed class GetByIdHandler<
    [DynamicallyAccessedMembers(
        DynamicallyAccessedMemberTypes.PublicConstructors
            | DynamicallyAccessedMemberTypes.NonPublicConstructors
            | DynamicallyAccessedMemberTypes.PublicFields
            | DynamicallyAccessedMemberTypes.NonPublicFields
            | DynamicallyAccessedMemberTypes.PublicProperties
            | DynamicallyAccessedMemberTypes.NonPublicProperties
            | DynamicallyAccessedMemberTypes.Interfaces
    )]
        TEntity
>(SampleDbContext db) : IQueryHandler<GetByIdQuery<TEntity>, OperationOutcome<TEntity, NotFound>>
    where TEntity : class
{
    /// <inheritdoc/>
    public async Task<OperationOutcome<TEntity, NotFound>> Handle(GetByIdQuery<TEntity> query, CancellationToken ct)
    {
        var entity = await db.Set<TEntity>().FindAsync([query.Id], ct);
        return entity is null ? new NotFound($"{typeof(TEntity).Name} {query.Id} not found") : entity;
    }
}
