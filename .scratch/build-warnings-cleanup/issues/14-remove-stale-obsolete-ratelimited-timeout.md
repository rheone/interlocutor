# 14 - Remove stale [Obsolete] from RateLimited/Timeout

**What to build:** `RateLimited.cs` and `Timeout.cs` each carry `[Obsolete("May be overkill to include")]` - a design-uncertainty note predating the `resulttypes-cleanup` tickets 10 and 11 (both done, merged), which shipped full `TimeoutBehavior`/`RateLimitedBehavior` pipeline behaviors that produce exactly these two cases as real outcomes. The "should this type exist?" question is already answered by that shipped code, so the attribute is stale. Remove `[Obsolete]` from both types entirely - no replacement attribute. (Actually removing the types, if ever decided, is a separate future decision, out of scope here.)

**Blocked by:** None - can start immediately.

**Status:** done

- [x] `[Obsolete]` attribute removed from `RateLimited` (`src/Interlocutor.CommonKernel/ResultTypes/RateLimited.cs`) and `Timeout` (`src/Interlocutor.CommonKernel/ResultTypes/Timeout.cs`)
- [x] No other change to either type's members, docs, or behavior
- [x] `dotnet build` reports zero `CS0618` warnings anywhere referencing `RateLimited`/`Timeout`
- [x] `dotnet test` passes for `Interlocutor.CommonKernel.Tests` and `Interlocutor.Abstractions.Tests`

## Comments

Already implemented in commit `56141a9`. Verified: neither `RateLimited` nor `Timeout` (`src/Interlocutor.CommonKernel/ResultTypes/`) carries an `[Obsolete]` attribute; no other member changed. A full rebuild reports zero `CS0618` warnings referencing either type anywhere in the solution. `Interlocutor.CommonKernel.Tests`/`Interlocutor.Abstractions.Tests` pass.
