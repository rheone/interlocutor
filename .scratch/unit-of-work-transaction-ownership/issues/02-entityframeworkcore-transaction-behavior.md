# 02 - EF Core: unit-of-work-owned transaction behavior

**What to build:** A command that writes - through `IDomainRepository`, through `IUnitOfWork`, or through a directly-injected `Lazy<TContext>` - automatically gets a transaction opened the first time it writes, committed if the pipeline's outcome classifier reports success, and rolled back on a classified failure or an explicit `Cancel`. No marker is required for this to happen. A dispatch that only reads never opens a transaction. `ITransactionalRequest` on a request forces the transaction to begin eagerly, at pipeline start, instead of waiting for the first write.

**Blocked by:** 01.

**Status:** done

- [x] New behavior, registered opt-in (not part of a bare `AddInterlocutor()` call), applies to every request once registered - no marker needed to participate
- [x] Transaction opens on first write regardless of whether the write came through `IDomainRepository`, `IUnitOfWork`, or direct `Lazy<TContext>` access
- [x] A dispatch that never writes never opens a transaction
- [x] Commit on classifier-reported success; rollback on classifier-reported failure
- [x] Calling `IUnitOfWork.Cancel` rolls back immediately; the handler's own returned outcome is preserved, not overridden by the behavior
- [x] A request implementing `ITransactionalRequest` gets an eager transaction begin at pipeline start rather than on first write
- [x] A request implementing `IExternalStoreRequest` never gets a local transaction, even if it touches the scoped `DbContext`

## Comments

Already implemented in commit `c938568` - `src/Interlocutor.Persistence.EntityFrameworkCore/UnitOfWorkBehavior.cs` matches every acceptance bullet: unconstrained `TRequest` (no marker needed), begin-on-first-write via `UnitOfWorkSaveChangesInterceptor`/`EntityFrameworkUnitOfWork.SaveChangesAsync` regardless of access path, eager begin for `ITransactionalRequest`, `IExternalStoreRequest` exclusion, and commit/rollback driven by the outcome classifier with `IUnitOfWork.Cancel` preserving the handler's own returned outcome. `tests/Interlocutor.Persistence.EntityFrameworkCore.Tests/UnitOfWorkBehaviorTests.cs` and `UnitOfWorkSaveChangesInterceptorTests.cs` cover it.
