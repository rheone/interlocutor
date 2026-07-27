namespace Mediance.CommonKernel;

public readonly union Result<T>(T, Failure);

public readonly union Result(Success, Failure);

public sealed record PagedResult<T>(IReadOnlyList<T> items, int page, int pageSize, long totalCount)
{
    public int TotalPages => this.pageSize <= 0 ? 0 : (int)Math.Ceiling(this.totalCount / (double)this.pageSize);

    public bool HasNextPage => (long)this.page * this.pageSize < this.totalCount;
}
