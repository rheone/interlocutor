# 02 - Rename existing non-contract Async-suffix violations in src/

**What to build:** The small, already-identified set of internal `src/` helper methods that return an awaitable type but don't carry the `Async` suffix get renamed to comply - independent of the dispatch-contract rename, since none of these are interface members.

**Blocked by:** None - can start immediately.

**Status:** ready-for-agent

- [ ] The EF Core unit-of-work behavior's internal pass-through orchestration helper, the pipeline chain's execution entry point, and the mediator's internal trace-wrapping helpers are renamed to end in `Async`.
- [ ] Every call site of these renamed helpers is updated to the new name.
- [ ] Solution builds and all existing tests pass unchanged - these are internal/private members, so there is no externally-visible API change.
