# 33 - S6966: prefer `IsDirtyAsync`/other async NHibernate members over their sync counterparts

**What to build:** SonarAnalyzer's S6966 ("Await '<AsyncMember>' instead") fires roughly a dozen times across `src/Interlocutor.Persistence.NHibernate/UnitOfWorkBehavior.cs` (`IsDirty()` → `IsDirtyAsync()`), `TutorialProject/TodoTasks.Api`, `tests/Interlocutor.Persistence.NHibernate.Tests`, `tests/Interlocutor.Persistence.EntityFrameworkCore.Tests`, and `tests/Interlocutor.DependencyInjection.Microsoft.EntityFrameworkCore.Tests`. Ticket 05 investigated this exact rule under SonarLint alone (no CLI analyzer at the time) and found no genuine sync-over-async blocking pattern via manual grep - now that a real analyzer flags specific call sites, re-examine each one: `UnitOfWorkBehavior<,>`'s own `IsDirty()` call is the highest-value fix (it already runs inside an `async` method, so switching to `IsDirtyAsync()` is a real, in-scope improvement), while occurrences inside synchronous test-fixture setup/teardown code or already-synchronous helper methods may not have an async context available to await into - decide per call site and record why any are left as-is.

**Blocked by:** None - can start immediately.

**Status:** ready-for-agent

- [ ] `UnitOfWorkBehavior<,>.Handle`'s `IsDirty()` call is switched to `IsDirtyAsync()` (it already runs in an async context)
- [ ] Every other flagged call site is switched to its async counterpart where an `async` context is available and doing so doesn't change behavior, or kept with a recorded reason (e.g. no async context available at that call site)
- [ ] `dotnet build` reports zero `S6966` warnings
- [ ] `dotnet test` passes for every affected project, with the same commit/rollback/dirty-tracking semantics as before
