# 10 - Fix `IUnitOfWork.Cancel()` disposal asymmetry

**What to build:** NHibernate's `IUnitOfWork.Cancel()` disposes its rolled-back transaction the same deterministic way EF Core's does — or, if leaving it undisposed is genuinely fine given NHibernate's session-disposal semantics, a comment records that decision so a future reader doesn't "fix" it into an inconsistency with EF Core.

Full technical detail: [findings/09-unitofwork-cancel-disposal-asymmetry.md](../findings/09-unitofwork-cancel-disposal-asymmetry.md).

**Blocked by:** None - can start immediately.

**Status:** done

- [x] Either `NHibernateUnitOfWork.Cancel()` explicitly disposes the `ITransaction` it rolled back (mirroring `EntityFrameworkUnitOfWork.Cancel()`), or a comment explains why it's intentionally left to the owning `ISession`'s disposal
- [x] The chosen behavior is consistent with `IUnitOfWork`'s XML doc claim of a uniform unit-of-work-owned transaction model across stores

## Comments

**2026-09-04** - Re-triaged while working the frontier: already implemented in commit `6a3f9ce`. NHibernateUnitOfWork.Cancel() now explicitly disposes the rolled-back ITransaction, mirroring EntityFrameworkUnitOfWork.Cancel()'s explicit disposal. No source changes were needed this session.
