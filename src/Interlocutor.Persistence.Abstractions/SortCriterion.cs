namespace Interlocutor.Persistence.Abstractions;

using System.Linq.Expressions;

/// <summary>
/// One entry of a multi-column sort specification passed to
/// <see cref="IDomainRepository{TEntity, TKey}.GetManyAsync"/> or
/// <see cref="IDomainRepository{TEntity, TKey}.GetPagedAsync"/>.
/// </summary>
/// <typeparam name="TEntity">The entity type being sorted.</typeparam>
/// <param name="KeySelector">The property to sort by, expressed as a lambda over <typeparamref name="TEntity"/>.</param>
/// <param name="Direction">The direction to apply for this sort key.</param>
/// <remarks>
/// A repository implementation composes an ordered list of these into a multi-column
/// <c>OrderBy</c>/<c>ThenBy</c> (or <c>OrderByDescending</c>/<c>ThenByDescending</c>) chain, applying each
/// entry in list order - the first entry is the primary sort key, the second breaks ties on the first, and
/// so on.
/// </remarks>
public sealed record SortCriterion<TEntity>(Expression<Func<TEntity, object>> KeySelector, SortDirection Direction);
