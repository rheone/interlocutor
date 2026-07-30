namespace Mediance.Abstractions.Commands;

/// <summary>
/// A command that produces a sequence of response items.
/// </summary>
/// <remarks>
/// <para>
/// Unlike <see cref="IBulkCommand{TItem,TResponseItem}"/>, <see cref="IEnumerableCommand{TResponseItem}"/>
/// is a marker-only interface: it declares neither input items nor any members beyond
/// the inherited <c>ICommand&lt;IReadOnlyList&lt;TResponseItem&gt;&gt;</c>. Handlers
/// derive the input from the command's own properties rather than receiving an explicit
/// item collection.
/// </para>
/// </remarks>
/// <typeparam name="TResponseItem">The type of each individual response item.</typeparam>
public interface IEnumerableCommand<TResponseItem> : ICommand<IReadOnlyList<TResponseItem>>;
