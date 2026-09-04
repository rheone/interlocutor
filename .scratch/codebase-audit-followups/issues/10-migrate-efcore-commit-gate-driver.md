# 10 - Migrate EF Core's UnitOfWorkBehavior onto the shared commit-gate driver

**What to build:** `Interlocutor.Persistence.EntityFrameworkCore`'s `UnitOfWorkBehavior` hand-rolls the same commit-gate algorithm as NHibernate's (bypass check, eager-begin, run next, check `ChangeTracker.HasChanges()`, commit-with-logging or rollback-with-logging). Migrate it onto the shared driver introduced in ticket 09, implementing only its own store-specific primitives (`HasChanges`, `BeginTransactionAsync`, etc.) behind the same seam.

**Blocked by:** 09 - Introduce a shared commit-gate driver in Persistence.Abstractions; migrate NHibernate onto it

**Status:** ready-for-agent

- [ ] `Interlocutor.Persistence.EntityFrameworkCore`'s `UnitOfWorkBehavior` implements only the store-specific primitives; the control flow lives in the shared driver from ticket 09
- [ ] `Interlocutor.Persistence.EntityFrameworkCore` still depends only on `Persistence.Abstractions`, and does not depend on `Interlocutor.Persistence.NHibernate` (verified by `tests/Interlocutor.Architecture.Tests`)
- [ ] Observable behavior is unchanged - `Interlocutor.Persistence.EntityFrameworkCore.Tests` (including `UnitOfWorkBehaviorTests`) pass without change to their assertions
- [ ] The two stores' `UnitOfWorkBehaviorTests.cs` files can share a common parameterized/abstract test base exercising the driver once, if this falls out naturally - not required if it doesn't
