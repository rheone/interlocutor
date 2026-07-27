namespace Mediance.Abstractions.Commands;

public interface IEnumerableCommand<TResponseItem> : ICommand<IReadOnlyList<TResponseItem>>;
