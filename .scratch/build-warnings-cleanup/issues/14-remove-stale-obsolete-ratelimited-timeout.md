# 14 - Remove stale [Obsolete] from RateLimited/Timeout

**What to build:** `RateLimited.cs` and `Timeout.cs` each carry `[Obsolete("May be overkill to include")]` - a design-uncertainty note predating the `resulttypes-cleanup` tickets 10 and 11 (both done, merged), which shipped full `TimeoutBehavior`/`RateLimitedBehavior` pipeline behaviors that produce exactly these two cases as real outcomes. The "should this type exist?" question is already answered by that shipped code, so the attribute is stale. Remove `[Obsolete]` from both types entirely - no replacement attribute. (Actually removing the types, if ever decided, is a separate future decision, out of scope here.)

**Blocked by:** None - can start immediately.

**Status:** ready-for-agent

- [ ] `[Obsolete]` attribute removed from `RateLimited` (`src/Interlocutor.CommonKernel/ResultTypes/RateLimited.cs`) and `Timeout` (`src/Interlocutor.CommonKernel/ResultTypes/Timeout.cs`)
- [ ] No other change to either type's members, docs, or behavior
- [ ] `dotnet build` reports zero `CS0618` warnings anywhere referencing `RateLimited`/`Timeout`
- [ ] `dotnet test` passes for `Interlocutor.CommonKernel.Tests` and `Interlocutor.Abstractions.Tests`
