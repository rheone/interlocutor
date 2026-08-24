namespace Interlocutor.Sample.Handlers.Shared;

using Interlocutor.Abstractions.Core;
using Interlocutor.CommonKernel;
using Interlocutor.CommonKernel.ResultTypes;
using Interlocutor.Sample.Handlers.Tags;
using Interlocutor.Sample.Handlers.Todos;

/// <summary>
/// One typed entry point per entity type <see cref="GetByIdQuery{TEntity}"/> is used for. Interlocutor.Generator
/// only closes an open generic handler over a request type when a closed usage of that instantiation is
/// visible in the same compilation as the handler - see <see cref="GetByIdHandler{TEntity}"/>'s remarks. Both
/// call sites live here, beside the handler they close, rather than at the web application's call sites, so
/// <c>GetByIdQuery&lt;TodoItem&gt;</c> and <c>GetByIdQuery&lt;TagItem&gt;</c> resolve through the compile-time
/// generated registration module instead of needing runtime scanning.
/// </summary>
public static class GetByIdQueries
{
    /// <summary>Looks up a <see cref="TodoItem"/> by primary key through the generic <see cref="GetByIdHandler{TEntity}"/>.</summary>
    /// <param name="sender">The sender to dispatch through.</param>
    /// <param name="id">The to-do item's primary key.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The outcome of the lookup.</returns>
    public static Task<OperationOutcome<TodoItem, NotFound>> GetTodoByIdGenericAsync(
        this ISender sender,
        int id,
        CancellationToken ct
    ) => sender.Send(new GetByIdQuery<TodoItem>(id), ct);

    /// <summary>Looks up a <see cref="TagItem"/> by primary key through the generic <see cref="GetByIdHandler{TEntity}"/>.</summary>
    /// <param name="sender">The sender to dispatch through.</param>
    /// <param name="id">The tag's primary key.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The outcome of the lookup.</returns>
    public static Task<OperationOutcome<TagItem, NotFound>> GetTagByIdAsync(
        this ISender sender,
        int id,
        CancellationToken ct
    ) => sender.Send(new GetByIdQuery<TagItem>(id), ct);
}
