# 04 - Failure/Failure&lt;TErrorCode&gt;/Failures/Failures&lt;TErrorCode&gt; redesign

## Labels

ready-for-agent

**What to build:** `Failure` today has a `long? ErrorCode` and three hand-written constructors (reason-only, code-only, both), which also disagrees with what `documentation/messages-and-result-types.md` already claims (`string? ErrorCode`). Redesign it to mirror the existing `ValidationError`/`ValidationError<TErrorCode>` symmetry: collapse non-generic `Failure` to a plain `(string? Reason = null)` positional record, matching `NotFound`/`Forbidden`'s shape, and add a new generic `Failure<TErrorCode>(TErrorCode ErrorCode, string? Reason = null)` where `TErrorCode : notnull, IEquatable<TErrorCode>`. Update `Failures` (aggregates non-generic `Failure`, built on the `DistinctItemSet<TItem>` from ticket 01) and add a new `Failures<TErrorCode>` aggregating `Failure<TErrorCode>`, structurally mirroring `ValidationErrors`/`ValidationErrors<TErrorCode>`. Add a `.Reasons` semantic projection to both aggregate types.

**Blocked by:** 01 (DistinctItemSet + plural-type rewrite)

- [ ] `Failure` is `readonly record struct Failure(string? Reason = null)` - the three old constructors and `long? ErrorCode`/`HasReason`/`HasErrorCode` members are removed
- [ ] `Failure<TErrorCode>` is a new type: `readonly record struct Failure<TErrorCode>(TErrorCode ErrorCode, string? Reason = null) where TErrorCode : notnull, IEquatable<TErrorCode>`
- [ ] `Failures` aggregates non-generic `Failure`, composes `DistinctItemSet<Failure>`, exposes `.Items`, `.Count`, and a `.Reasons` semantic projection; `.Values` is removed (per ticket 01)
- [ ] New `Failures<TErrorCode>` aggregates `Failure<TErrorCode>`, mirroring `ValidationErrors<TErrorCode>`'s shape: `.Items`, `.Count`, `.Reasons`, `.ErrorCodes`
- [ ] `documentation/messages-and-result-types.md`'s `Failure` code sample is left as-is for now (doc pass is ticket 15) but the shipped type now matches what that doc already claims
- [ ] Every call site of the old `Failure` constructors/members in `src/` and `tests/` is updated to the new shape (search the whole solution, not just CommonKernel)
- [ ] Unit tests cover construction, equality, and default-value behavior for `Failure`, `Failure<TErrorCode>`, `Failures`, `Failures<TErrorCode>`
- [ ] `dotnet build` (whole solution) and `dotnet test tests/Interlocutor.CommonKernel.Tests/Interlocutor.CommonKernel.Tests.csproj` pass
