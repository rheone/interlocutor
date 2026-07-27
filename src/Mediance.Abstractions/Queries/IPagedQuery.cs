using Mediance.CommonKernel;

namespace Mediance.Abstractions.Queries;

public interface IPagedQuery<TItem> : IQuery<PagedResult<TItem>>
{
    int Page { get; }

    int PageSize { get; }
}
