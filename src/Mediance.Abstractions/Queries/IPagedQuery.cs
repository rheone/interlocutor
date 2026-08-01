//using Mediance.CommonKernel;

//namespace Mediance.Abstractions.Queries;

///// <summary>
///// Represents a query that returns a paginated subset of results.
///// </summary>
///// <typeparam name="TItem">The type of items in each page of results.</typeparam>
//public interface IPagedQuery<TItem> : IQuery<PagedResult<TItem>>
//{
//    /// <summary>
//    /// Gets the one-based index of the requested page.
//    /// </summary>
//    /// <value>The one-based page number. Must be greater than or equal to 1.</value>
//    int Page { get; }

//    /// <summary>
//    /// Gets the maximum number of items per page.
//    /// </summary>
//    /// <value>The maximum number of items per page. Must be greater than zero.</value>
//    int PageSize { get; }
//}
