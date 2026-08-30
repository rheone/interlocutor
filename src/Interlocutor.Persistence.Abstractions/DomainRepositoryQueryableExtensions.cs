namespace Interlocutor.Persistence.Abstractions;

using System.Linq.Expressions;

/// <summary>
/// Store-agnostic composition of an <see cref="IDomainRepository{TEntity, TKey}"/> filter/sort/paging
/// specification onto an <see cref="IQueryable{T}"/>, shared by every implementation so filter/sort/paging
/// is composed once, on the query, and pushed down to the store - never materialized first.
/// </summary>
/// <remarks>
/// Every method here composes plain <see cref="System.Linq.Queryable"/> calls, which both EF Core's and
/// NHibernate's LINQ providers translate to SQL. None of these methods enumerate their query argument -
/// each returns a new, still-unevaluated <see cref="IQueryable{T}"/>; only the caller's own materialization
/// (an ORM-specific <c>ToListAsync</c>/<c>CountAsync</c>) executes it.
/// </remarks>
public static class DomainRepositoryQueryableExtensions
{
    extension<TEntity>(IQueryable<TEntity> query)
    {
        /// <summary>
        /// Applies an optional filter predicate to <paramref name="query"/>.
        /// </summary>
        /// <param name="filter">The predicate to apply, or <see langword="null"/> to leave <paramref name="query"/> unchanged.</param>
        /// <returns><paramref name="query"/> with <paramref name="filter"/> applied via <c>Queryable.Where</c>, if given.</returns>
        public IQueryable<TEntity> ApplyFilter(Expression<Func<TEntity, bool>>? filter) =>
            filter is null ? query : query.Where(filter);

        /// <summary>
        /// Applies an optional multi-column sort specification to <paramref name="query"/>.
        /// </summary>
        /// <param name="sort">
        /// The ordered sort criteria to apply - the first entry becomes the primary <c>OrderBy</c>/<c>OrderByDescending</c>
        /// key, each subsequent entry a <c>ThenBy</c>/<c>ThenByDescending</c> tiebreaker - or <see langword="null"/>/empty
        /// to leave <paramref name="query"/>'s ordering unspecified.
        /// </param>
        /// <returns><paramref name="query"/> with <paramref name="sort"/> applied, if given.</returns>
        public IQueryable<TEntity> ApplySort(IReadOnlyList<SortCriterion<TEntity>>? sort)
        {
            if (sort is null || sort.Count == 0)
            {
                return query;
            }

            IOrderedQueryable<TEntity>? ordered = null;
            foreach (var criterion in sort)
            {
                if (ordered is null)
                {
                    if (criterion.Direction == SortDirection.Ascending)
                    {
                        ordered = query.OrderBy(criterion.KeySelector);
                    }
                    else
                    {
                        ordered = query.OrderByDescending(criterion.KeySelector);
                    }
                }
                else if (criterion.Direction == SortDirection.Ascending)
                {
                    ordered = ordered.ThenBy(criterion.KeySelector);
                }
                else
                {
                    ordered = ordered.ThenByDescending(criterion.KeySelector);
                }
            }

            return ordered!;
        }

        /// <summary>
        /// Applies a one-based page number and page size to <paramref name="query"/> via <c>Skip</c>/<c>Take</c>.
        /// Callers apply <see cref="DomainRepositoryQueryableExtensions.ApplyFilter{TEntity}(IQueryable{TEntity}, Expression{Func{TEntity, bool}}?)"/>
        /// and <see cref="DomainRepositoryQueryableExtensions.ApplySort{TEntity}(IQueryable{TEntity}, IReadOnlyList{SortCriterion{TEntity}}?)"/> first.
        /// </summary>
        /// <param name="pageNumber">The one-based page number to retrieve. Must be at least <c>1</c>.</param>
        /// <param name="pageSize">The maximum number of items per page. Must be at least <c>1</c>.</param>
        /// <returns>A query restricted to the requested page.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="pageNumber"/> or <paramref name="pageSize"/> is less than <c>1</c>.
        /// </exception>
        public IQueryable<TEntity> ApplyPaging(int pageNumber, int pageSize)
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(pageNumber, 1);
            ArgumentOutOfRangeException.ThrowIfLessThan(pageSize, 1);

            return query.Skip((pageNumber - 1) * pageSize).Take(pageSize);
        }
    }
}
