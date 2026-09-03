Status: needs-triage
Severity: MEDIUM
Category: architecture
Source: architecture.md lane, confirmed by antagonist lane (real-world blast radius noted as smaller than "resource leak" framing suggests)

# `IUnitOfWork.Cancel()` disposes the rolled-back transaction in EF Core but not in NHibernate

## Summary

The two `IUnitOfWork` implementations handle the same documented "rare-case escape hatch" (`Cancel()`) inconsistently: EF Core rolls back *and* disposes the transaction object it retrieved; NHibernate only rolls back.

## Evidence

`EntityFrameworkUnitOfWork.Cancel()` (lines 55-65) does `await transaction.RollbackAsync(...)` then `await transaction.DisposeAsync()`. `NHibernateUnitOfWork.Cancel()` (lines 54-63) does `await transaction.RollbackAsync(...)` only — the `ITransaction` object from `GetCurrentTransaction()` is never disposed. Per both `UnitOfWorkBehavior`'s own doc comments ("...or an explicit `IUnitOfWork.Cancel` already cleaned up..."), a cancelled dispatch is treated as fully finalized and nothing else in the pipeline touches the transaction again.

Note: NHibernate's `ITransaction` typically wraps the ADO.NET `DbTransaction`, and the owning `ISession`'s disposal at end of DI scope will eventually release it — this is not a definite leak the way an undisposed `DbConnection` would be, just an inconsistency with the EF Core side's explicit, deterministic disposal.

## Suggested action

Either dispose the NHibernate transaction explicitly in `Cancel()` (mirroring the EF Core side, with the same disposal-analyzer suppression/justification comment pattern used elsewhere in these two files), or, if leaving it undisposed is intentional, add a comment explaining the asymmetry so a future reader doesn't "fix" it into an inconsistency with EF Core, or vice versa.
