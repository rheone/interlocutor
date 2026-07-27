namespace Mediance.Abstractions.Queries;

public interface IEnumerableQuery<TItem> : IQuery<IReadOnlyList<TItem>>;
