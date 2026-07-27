namespace Mediance.CommonKernel;

public sealed record Success;

public sealed record ValidationFailed(
    IReadOnlyDictionary<string, IReadOnlyList<(string Code, string Message)>> errors);

public sealed record RuleViolation(string code, string message);

public sealed record NotFound(string entityType, object key);

public sealed record Forbidden(string? reason = null);

public sealed record Conflict(string code, string message);

public readonly union Failure(ValidationFailed, RuleViolation, NotFound, Forbidden, Conflict);

public readonly union Result<T>(T, Failure);

public readonly union Result(Success, Failure);

public sealed record PagedResult<T>(IReadOnlyList<T> items, int page, int pageSize, long totalCount)
{
    public int TotalPages => this.pageSize <= 0 ? 0 : (int)Math.Ceiling(this.totalCount / (double)this.pageSize);

    public bool HasNextPage => (long)this.page * this.pageSize < this.totalCount;
}
