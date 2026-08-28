> **Status: done** - merged in `341bbec`.

# 06 - Uniform ToString() for existing case types

## Labels

ready-for-agent

**What to build:** None of the pre-existing ResultTypes have a `ToString()` override today (an earlier attempt on `Failure` was abandoned and left commented out). Add a uniform convention across every remaining ResultTypes struct: `"TypeName: detail"` when a detail is present, bare `"TypeName"` otherwise, overriding per-type where a single-detail shape doesn't fit (e.g. `Conflict` has two mandatory fields; the plural aggregate types should join their primary semantic projection, not nest each item's own `ToString()`, to avoid doubled prefixes like `"Conflicts: Conflict: ..."`).

**Blocked by:** 01 (DistinctItemSet + plural-type rewrite), 04 (Failure/Failures redesign)

- [x] `Success` → `"Success"`
- [x] `Failure` → `"Failure"` / `"Failure: {Reason}"`; `Failure<TErrorCode>` → includes `ErrorCode` and optional `Reason`
- [x] `NotFound` → `"NotFound"` / `"NotFound: {Message}"`; `NotFound<TId>` → includes `EntityType`/`Id` and optional `Message` (this generic variant does exist in this codebase, in `NotFound.cs`)
- [x] `Forbidden` → `"Forbidden"` / `"Forbidden: {Reason}"`
- [x] `Conflict` → includes both `Code` and `Message` (both mandatory)
- [x] `ValidationError` → includes `Message`; `ValidationError<TErrorCode>` → includes `Message` and `ErrorCode`
- [x] `Conflicts`, `Failures`, `Failures<TErrorCode>`, `ValidationErrors`, `ValidationErrors<TErrorCode>` → empty set formats as bare type name; non-empty joins the primary semantic projection (not nested item `ToString()`)
- [x] The dead commented-out `ToString()` block in `Failure.cs`'s history is fully replaced (no leftover commented code)
- [x] Unit tests cover both the "has detail" and "bare" branches for every type
- [x] `dotnet test tests/Interlocutor.CommonKernel.Tests/Interlocutor.CommonKernel.Tests.csproj` passes
