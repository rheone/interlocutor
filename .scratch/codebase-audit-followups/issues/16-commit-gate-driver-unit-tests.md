# 16 - Add direct unit tests for CommitGateDriver/ICommitGateSession

**What to build:** `CommitGateDriver`'s begin/run-next/check-pending-work/commit-or-rollback-with-logging algorithm currently has no test of its own in `Interlocutor.Persistence.Abstractions.Tests` - it's covered only indirectly, through NHibernate's and EF Core's own `UnitOfWorkBehaviorTests`. Add direct tests exercising the driver against a mock/fake `ICommitGateSession` and a mocked `next()` delegate, per `documentation/testing.md`'s "mock the `next()` delegate" guidance, so the shared algorithm's behavior is verified once at the seam that owns it rather than only through two stores' worth of indirection.

**Blocked by:** None - can start immediately

**Status:** done

- [x] A test exists asserting the bypass path: an `IExternalStoreRequest` never causes `sessionFactory` to be invoked
- [x] A test exists asserting eager-begin: an `ITransactionalRequest` causes `BeginTransactionAsync` to be called before `next()` runs
- [x] A test exists asserting the commit path: when `HasPendingWorkAsync` reports pending work after `next()` completes, `CommitAsync` is called and not `RollbackAsync`
- [x] A test exists asserting the rollback path: when there is no pending work (or `next()` throws), `RollbackAsync` is called and not `CommitAsync`
- [x] A test exists asserting logging occurs on both the commit and rollback paths, per the existing describer/logger contract
- [x] `Interlocutor.Persistence.Abstractions.Tests`, and the full solution test suite, pass

## Comments

**2026-09-04** - Verified on `2026-09-04-ResearchTicketNormalization` after merging `origin/2026-09-04-CodebaseAuditFollowUps` and `origin/ultrareview`: `CommitGateDriverTests.cs` (306 lines) added directly to `Interlocutor.Persistence.Abstractions.Tests`, covering the driver at its own seam. Full solution `dotnet build` and `dotnet test` (1189/1189) pass post-merge.
