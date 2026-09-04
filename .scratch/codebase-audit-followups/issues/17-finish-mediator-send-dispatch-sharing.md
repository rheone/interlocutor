# 17 - Finish extracting Mediator.Send's cache-lookup/wrapper-resolution duplication

**What to build:** Ticket 13 extracted `Mediator`'s three `Send<T>` overloads' shared activity-start/fast-path/`TraceAsync` branching into `DispatchWithTrace`, but each overload still hand-duplicates the `ConcurrentDictionary` cache lookup and `Activator.CreateInstance` wrapper resolution that sits around it - the larger share of the original duplication. Finish the extraction so each overload supplies only its own cache, key, and wrapper type to one shared helper, per ticket 13's original (partially-met) acceptance criterion.

**Blocked by:** None - can start immediately

**Status:** done

- [x] The cache lookup (`GetOrAdd`) and `Activator.CreateInstance` wrapper construction are performed by one shared helper, not re-typed in each of the three `Send<T>` overloads
- [x] Each overload supplies only its own cache, key, and wrapper type/interface to that helper
- [x] Dispatch semantics are unchanged for all three overloads (query, command-with-result, void-command)
- [x] `Interlocutor.Abstractions.Tests` (`MediatorTests.cs`), and the full solution test suite, pass without change to their assertions

## Comments

**2026-09-04** - Verified on `2026-09-04-ResearchTicketNormalization` after merging `origin/2026-09-04-CodebaseAuditFollowUps` and `origin/ultrareview`: `DispatchWithTrace<TKey, TWrapper, TResponse>` now centralizes the `ConcurrentDictionary.GetOrAdd` cache lookup and `Activator.CreateInstance` wrapper resolution for all three `Send` overloads, completing ticket 13's partial extraction. Full solution `dotnet build` and `dotnet test` (1189/1189) pass post-merge.
