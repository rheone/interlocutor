## Labels

ready-for-agent

## What to build

Improve `TransactionBehavior` in `Interlocutor.EntityFrameworkCore` in two ways:

**1. Explicit error handling around SaveChanges/Commit**
Currently, if `SaveChangesAsync` throws, the exception propagates past `CommitAsync` and rolls back via `DisposeAsync`. Add an explicit `try`/`catch` that rolls back the transaction and re-throws, making the error path predictable and testable.

**2. IExternalStoreRequest handling**
The `ITransactionalRequest` opt-in marker already gates whether a transaction begins. The `IExternalStoreRequest` marker exists but is never checked - it should serve as an opt-out override. If a request implements `IExternalStoreRequest`, skip the transaction even if it also implements `ITransactionalRequest`.

The guard logic should be:
```
if (request is not ITransactionalRequest || request is IExternalStoreRequest)
{
    return await next();  // no transaction
}
```

## Acceptance criteria

- [ ] `SaveChangesAsync` failure triggers explicit rollback (not just DisposeAsync)
- [ ] `IExternalStoreRequest` requests skip transaction and session resolution
- [ ] Non-transactional/non-external requests still behave identically
- [ ] Tests cover: SaveChanges failure path, external-store skip, normal transactional flow
- [ ] All existing tests still pass

## Blocked by

None - can start immediately
