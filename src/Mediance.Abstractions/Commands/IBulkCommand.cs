#warning remove?

//namespace Mediance.Abstractions.Commands;

///// <summary>
///// A command that operates on a batch of items and produces one response per item.
///// </summary>
///// <typeparam name="TItem">The type of each individual item in the batch.</typeparam>
///// <typeparam name="TResponseItem">The type of each individual response item.</typeparam>
//public interface IBulkCommand<TItem, TResponseItem> : ICommand<IReadOnlyList<TResponseItem>>
//{
//    /// <summary>
//    /// Gets the collection of items to process in the bulk operation.
//    /// </summary>
//    /// <value>The items to process, each producing a corresponding entry in the response list.</value>
//    IReadOnlyList<TItem> Items { get; }
//}
