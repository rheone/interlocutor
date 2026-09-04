# 03 - Regression-test transaction correctness for null-payload successes

**What to build:** A handler that succeeds with a `null` case payload has its transaction **committed**, not rolled back, under both EF Core's and NHibernate's `UnitOfWorkBehavior` — including when the null-payload success flows through `ResourceCommandHandler`, not just through a bare hand-constructed `Outcome<...>`. Also confirms the same request is audited/logged as a success, not a failure.

Full technical detail: [findings/01-outcome-null-payload-no-discriminator.md](../findings/01-outcome-null-payload-no-discriminator.md) ("Blast radius" section).

**Blocked by:** 01 - Fix the `Outcome` null-payload/no-discriminator bug at the root.

**Status:** done

- [x] A test proves a null-payload success commits under `Interlocutor.Persistence.EntityFrameworkCore`'s `UnitOfWorkBehavior`
- [x] A test proves a null-payload success commits under `Interlocutor.Persistence.NHibernate`'s `UnitOfWorkBehavior`
- [x] A test proves the same for a null-payload success routed through `ResourceCommandHandler`
- [x] A test proves `AuditingBehavior`/`LoggingBehavior` record such a request as a success, not a failure

## Comments

**2026-09-04** - Re-triaged while working the frontier: already implemented in commit `6a3f9ce`. tests/Interlocutor.Persistence.EntityFrameworkCore.Tests/UnitOfWorkBehaviorTests.cs and the NHibernate equivalent both have Handle_NullPayloadSuccess_CommitsNotRollsBack and Handle_NullPayloadSuccessRoutedThroughResourceCommandHandler_CommitsNotRollsBack tests, proving commit (not rollback) and correct Auditing/Logging classification. No source changes were needed this session.
