namespace Interlocutor;

// ---------------------------------------------------------------------------
// Package: Interlocutor
// The shared kernel (Addendum B.1). Depends on nothing but the BCL, so any
// consumer - console tool, worker, web API - can reference just this
// package. Every other package in the family (Interlocutor.Mediation and its
// satellites) depends on this one; this one depends on nothing in the family.
// ---------------------------------------------------------------------------

/// <summary>Marks the successful completion of a no-payload command.</summary>
public sealed record Success;

/// <summary>
/// Input/shape validation failed, before any lookup or business logic ran.
/// Distinct from <see cref="RuleViolation"/> - see that type's remarks.
/// </summary>
/// <param name="Errors">Field name mapped to the list of (code, message) failures for that field.</param>
public sealed record ValidationFailed(
    IReadOnlyDictionary<string, IReadOnlyList<(string Code, string Message)>> Errors);

/// <summary>
/// A domain/business invariant was violated after the input was
/// structurally valid - e.g. "insufficient balance," "invalid state
/// transition." Kept distinct from <see cref="ValidationFailed"/> because
/// the two occur at different pipeline stages and map to different HTTP
/// semantics (400 vs 422).
/// </summary>
public sealed record RuleViolation(string Code, string Message);

/// <summary>The entity a request targets does not exist. Never used to mean "zero rows returned by a list query."</summary>
public sealed record NotFound(string EntityType, object Key);

/// <summary>
/// A resource-based or policy-based authorization check denied the
/// request. Authentication is assumed to already have happened upstream;
/// this means "who you are is known, but you may not do this."
/// </summary>
public sealed record Forbidden(string? Reason = null);

/// <summary>The requested operation conflicts with current resource state (concurrency, uniqueness, invalid transition).</summary>
public sealed record Conflict(string Code, string Message);

/// <summary>
/// The closed set of every failure outcome. A C# 15 union: any
/// <c>switch</c> over this type is compiler-checked for exhaustiveness.
/// This is the single failure vocabulary shared by every
/// <see cref="Result{T}"/> and <see cref="Result"/> in the family, which is
/// what lets one mapping function (see <c>Interlocutor.AspNetCore</c>)
/// serve every request type.
/// </summary>
public union Failure(ValidationFailed, RuleViolation, NotFound, Forbidden, Conflict);

/// <summary>
/// The outcome of a query, or a value-returning command: either the
/// success payload <typeparamref name="T"/>, or a <see cref="Failure"/>.
/// </summary>
public union Result<T>(T, Failure);

/// <summary>The outcome of a no-payload command: either <see cref="Success"/>, or a <see cref="Failure"/>.</summary>
public union Result(Success, Failure);

/// <summary>A materialized page of results plus paging metadata. See <see cref="Interlocutor.Mediation"/>'s <c>IPagedQuery&lt;T&gt;</c> for the paired query contract.</summary>
public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, long TotalCount)
{
    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasNextPage => (long)Page * PageSize < TotalCount;
}
