# 10 - Migrate EF Core's UnitOfWorkBehavior onto the shared commit-gate driver

**What to build:** `Interlocutor.Persistence.EntityFrameworkCore`'s `UnitOfWorkBehavior` hand-rolls the same commit-gate algorithm as NHibernate's (bypass check, eager-begin, run next, check `ChangeTracker.HasChanges()`, commit-with-logging or rollback-with-logging). Migrate it onto the shared driver introduced in ticket 09, implementing only its own store-specific primitives (`HasChanges`, `BeginTransactionAsync`, etc.) behind the same seam.

**Blocked by:** 09 - Introduce a shared commit-gate driver in Persistence.Abstractions; migrate NHibernate onto it

**Status:** done

- [x] `Interlocutor.Persistence.EntityFrameworkCore`'s `UnitOfWorkBehavior` implements only the store-specific primitives; the control flow lives in the shared driver from ticket 09
- [x] `Interlocutor.Persistence.EntityFrameworkCore` still depends only on `Persistence.Abstractions`, and does not depend on `Interlocutor.Persistence.NHibernate` (verified by `tests/Interlocutor.Architecture.Tests`)
- [x] Observable behavior is unchanged - `Interlocutor.Persistence.EntityFrameworkCore.Tests` (including `UnitOfWorkBehaviorTests`) pass without change to their assertions
- [x] The two stores' `UnitOfWorkBehaviorTests.cs` files can share a common parameterized/abstract test base exercising the driver once, if this falls out naturally - not required if it doesn't

## Comments

**2026-09-04** - Verified on `2026-09-04-ResearchTicketNormalization` after merging `origin/2026-09-04-CodebaseAuditFollowUps` and `origin/ultrareview`: EF Core's `UnitOfWorkBehavior` also delegates to `CommitGateDriver.Handle` via `EntityFrameworkCommitGateSession`, landed in the same merge as ticket 09. Full solution `dotnet build` and `dotnet test` (1189/1189) pass post-merge.
