namespace Interlocutor.Persistence.Abstractions;

using System.Diagnostics;

/// <summary>
/// One materialized page of results returned by <see cref="IDomainRepository{TEntity, TKey}.GetPagedAsync"/>,
/// together with the paging metadata needed to render pagination controls.
/// </summary>
/// <typeparam name="T">The item type contained in the page.</typeparam>
/// <param name="Items">The items belonging to <paramref name="PageNumber"/>.</param>
/// <param name="PageNumber">The one-based page number this page represents.</param>
/// <param name="PageSize">The maximum number of items per page that was requested.</param>
/// <param name="TotalCount">The total number of items across every page, ignoring paging.</param>
/// <remarks>
/// <para>
/// This is a different type, for a different concern, than the consumer-defined <c>PagedResult&lt;T&gt;</c>
/// documented in <c>documentation/glossary.md</c>: that type is a consumer's own success-case payload for an
/// <c>Outcome&lt;PagedResult&lt;T&gt;, ...&gt;</c>, and <c>Interlocutor.CommonKernel</c> still ships
/// no opinion about pagination. <see cref="Page{T}"/> is this package's repository-return-shape vocabulary -
/// a consumer's handler is free to map it into its own success-case type, or return it unwrapped.
/// </para>
/// </remarks>
[DebuggerDisplay("Page {PageNumber}/{TotalPageCount}, {Items.Count} of {TotalCount} items")]
public sealed record Page<T>(
    [property: DebuggerBrowsable(DebuggerBrowsableState.RootHidden)] IReadOnlyList<T> Items,
    int PageNumber,
    int PageSize,
    int TotalCount
)
{
    /// <summary>Gets the total number of pages, given <see cref="TotalCount"/> and <see cref="PageSize"/>.</summary>
    /// <value>
    /// <c>0</c> when <see cref="TotalCount"/> is <c>0</c>; otherwise the ceiling of <see cref="TotalCount"/>
    /// divided by <see cref="PageSize"/>.
    /// </value>
    public int TotalPageCount => this.TotalCount == 0 ? 0 : ((this.TotalCount - 1) / this.PageSize) + 1;
}
