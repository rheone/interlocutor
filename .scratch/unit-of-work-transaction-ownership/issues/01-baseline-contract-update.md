# 01 - Baseline contract update

**What to build:** `IUnitOfWork` gains an explicit `Cancel` operation a handler can call to force an immediate rollback while keeping control of what outcome it returns afterward - the pipeline's commit gate simply finds nothing pending at completion. `ITransactionalRequest`'s documented meaning narrows from "this request wants a transaction" to "this request's read(s) and later write(s) must be protected by one eager transaction from pipeline start" - it no longer means "wants a transaction at all," since every write gets one automatically once later tickets land. `IExternalStoreRequest`'s meaning and mechanism are confirmed unchanged. No provider implements any of this yet - this ticket is the shared contract only.

**Blocked by:** None - can start immediately.

**Status:** done

- [x] `IUnitOfWork` exposes a `Cancel` operation, documented as immediate rollback that returns control to the caller (not a short-circuit/exception)
- [x] `ITransactionalRequest`'s XML docs describe the narrowed "eager begin for read-then-write atomicity" meaning, referencing ADR-0018
- [x] `IExternalStoreRequest`'s XML docs confirm no change to its meaning or mechanism
- [x] `Interlocutor.Persistence.Abstractions` and marker-interface tests still pass with the updated contract

## Comments

Already implemented, in commit `c938568` ("feat: make IUnitOfWork the transaction owner (ADR-0018)") - discovered already-done while starting this ticket, not built fresh here. Verified against the current tree: `IUnitOfWork.Cancel(CancellationToken)` exists, documented as immediate rollback that returns control to the caller (`src/Interlocutor.Persistence.Abstractions/IUnitOfWork.cs`); `ITransactionalRequest`'s XML docs describe the narrowed eager-begin meaning and reference `maintainer_docs/adr/0018-unit-of-work-as-transaction-owner.md`; `IExternalStoreRequest`'s docs are unchanged. `dotnet build -t:Rebuild`/`dotnet test` both clean across the whole solution.
