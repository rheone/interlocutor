# 09 - Introduce a shared commit-gate driver in Persistence.Abstractions; migrate NHibernate onto it

**What to build:** `Interlocutor.Persistence.NHibernate`'s `UnitOfWorkBehavior` hand-rolls the full commit-gate algorithm: bypass check for external-store requests, eager-begin transaction for `ITransactionalRequest`, run the next pipeline delegate, check for pending work (`ISession.IsDirty()`), then commit-with-logging or rollback-with-logging. `Interlocutor.Persistence.EntityFrameworkCore` will hand-roll the identical shape in a follow-up ticket. Extract the algorithm skeleton into `Interlocutor.Persistence.Abstractions` as a shared driver running against a small store-facing seam (members like `HasPendingWork`, `BeginTransactionAsync`, `CommitAsync`, `RollbackAsync`), and have NHibernate's `UnitOfWorkBehavior` implement only the store-specific primitives behind that seam.

This does not contradict ADR-0018 - that ADR concerns each store's advanced-surface member additions (lock modes, stateless sessions, etc.), not the shared baseline commit/rollback skeleton, and this ticket does not create a dependency between the two store packages.

**Blocked by:** None - can start immediately

**Status:** ready-for-agent

- [ ] `Interlocutor.Persistence.Abstractions` gains a shared commit-gate driver and a store-facing seam it drives
- [ ] `Interlocutor.Persistence.NHibernate`'s `UnitOfWorkBehavior` implements only the store-specific primitives; the begin/run/check/commit/rollback/log control flow lives in the shared driver
- [ ] `Interlocutor.Persistence.NHibernate` still depends only on `Persistence.Abstractions` (verified by `tests/Interlocutor.Architecture.Tests`)
- [ ] Observable behavior is unchanged - `Interlocutor.Persistence.NHibernate.Tests` (including `UnitOfWorkBehaviorTests`, `SharedSessionIntegrationTests`) pass without change to their assertions
