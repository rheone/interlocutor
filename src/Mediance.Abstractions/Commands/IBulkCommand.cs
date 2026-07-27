namespace Mediance.Abstractions.Commands;

public interface IBulkCommand<TItem, TResponseItem> : ICommand<IReadOnlyList<TResponseItem>>
{
    IReadOnlyList<TItem> Items { get; }
}
