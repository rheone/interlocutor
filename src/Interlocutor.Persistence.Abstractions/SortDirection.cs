namespace Interlocutor.Persistence.Abstractions;

/// <summary>
/// The direction to sort by for one entry of a <see cref="IDomainRepository{TEntity, TKey}"/> sort specification.
/// </summary>
public enum SortDirection
{
    /// <summary>Sort from the lowest value to the highest.</summary>
    Ascending,

    /// <summary>Sort from the highest value to the lowest.</summary>
    Descending,
}
