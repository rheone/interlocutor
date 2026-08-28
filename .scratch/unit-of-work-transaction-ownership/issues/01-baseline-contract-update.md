# 01 - Baseline contract update

**What to build:** `IUnitOfWork` gains an explicit `Cancel` operation a handler can call to force an immediate rollback while keeping control of what outcome it returns afterward - the pipeline's commit gate simply finds nothing pending at completion. `ITransactionalRequest`'s documented meaning narrows from "this request wants a transaction" to "this request's read(s) and later write(s) must be protected by one eager transaction from pipeline start" - it no longer means "wants a transaction at all," since every write gets one automatically once later tickets land. `IExternalStoreRequest`'s meaning and mechanism are confirmed unchanged. No provider implements any of this yet - this ticket is the shared contract only.

**Blocked by:** None - can start immediately.

**Status:** ready-for-agent

- [ ] `IUnitOfWork` exposes a `Cancel` operation, documented as immediate rollback that returns control to the caller (not a short-circuit/exception)
- [ ] `ITransactionalRequest`'s XML docs describe the narrowed "eager begin for read-then-write atomicity" meaning, referencing ADR-0018
- [ ] `IExternalStoreRequest`'s XML docs confirm no change to its meaning or mechanism
- [ ] `Interlocutor.Persistence.Abstractions` and marker-interface tests still pass with the updated contract
