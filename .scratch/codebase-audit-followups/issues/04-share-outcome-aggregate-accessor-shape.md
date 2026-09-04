# 04 - Share the Failures/Conflicts/ValidationErrors accessor shape

**What to build:** `Failures`/`Failures<TErrorCode>`, `Conflicts`/`Conflicts<TErrorCode>`, and `ValidationErrors`/`ValidationErrors<TErrorCode>` each independently retype the same accessor shape over their internal `DistinctItemSet<T>`: a `Count` passthrough, an `Items` passthrough, a distinct-projection (`Reasons`/`Messages`), and an identical `ToString()` pattern. Give this shape a single home - e.g. shared extension members on `DistinctItemSet<T>` - so the six case-aggregate types call the shared implementation instead of each retyping it.

Note: these are `readonly record struct` types with no inheritance available, so the fix is extension members / composition, not a base class.

**Blocked by:** None - can start immediately

**Status:** ready-for-agent

- [ ] `Count`, `Items`, the distinct-projection, and `ToString()` for all six types are backed by one shared implementation
- [ ] Each type's public API (property names, `ToString()` output format) is unchanged - this is an internal deduplication, not a breaking change
- [ ] `Interlocutor.CommonKernel.Tests` pass without modification to test expectations
- [ ] `tests/Interlocutor.Architecture.Tests` still passes (CommonKernel remains zero-dependency)
