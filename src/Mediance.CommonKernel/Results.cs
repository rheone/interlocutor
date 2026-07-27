namespace Mediance.CommonKernel;

public sealed record Success;

public sealed record ValidationFailed(
    IReadOnlyDictionary<string, IReadOnlyList<(string Code, string Message)>> Errors);

public sealed record RuleViolation(string Code, string Message);

public sealed record NotFound(string EntityType, object Key);

public sealed record Forbidden(string? Reason = null);

public sealed record Conflict(string Code, string Message);

public readonly union Failure(ValidationFailed, RuleViolation, NotFound, Forbidden, Conflict);

public readonly union Result<T>(T, Failure);

public readonly union Result(Success, Failure);

public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, long TotalCount)
{
    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);

    public bool HasNextPage => (long)Page * PageSize < TotalCount;
}
