## Labels

ready-for-agent

## What to build

Add test coverage for the commit/rollback logic in `TransactionBehavior<TRequest,TResponse,TContext>` (`src/Interlocutor.EntityFrameworkCore/TransactionBehavior.cs`). The behavior itself already implements the classifier-driven commit/rollback decision and the explicit try/catch-and-rethrow around `SaveChangesAsync`, but `tests/Interlocutor.EntityFrameworkCore.Tests/TransactionBehaviorTests.cs` only covers the `IExternalStoreRequest` skip path and a reflection-based generic-constraint check. The three paths that make this behavior worth having are completely untested.

## Acceptance criteria

- [ ] A handler returning a classified success commits the transaction and persists changes via `SaveChangesAsync`
- [ ] A handler returning a classified failure outcome rolls back the transaction without calling `SaveChangesAsync` a second time or swallowing the result
- [ ] `SaveChangesAsync` throwing rolls back the transaction and rethrows the original exception (not a wrapped one)
- [ ] Existing `IExternalStoreRequest` skip-path and generic-constraint tests still pass

## Blocked by

None - can start immediately
