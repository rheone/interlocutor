## Labels

ready-for-agent

## What to build

Add test coverage for the commit/rollback logic in `src/Interlocutor.NHibernate/TransactionBehavior.cs`, mirroring ticket 02's EF Core coverage. `tests/Interlocutor.NHibernate.Tests/TransactionBehaviorTests.cs` currently only covers the same two cases as its EF Core counterpart did before that ticket - the `IExternalStoreRequest` skip path and a generic-constraint check - leaving the actual commit/rollback decision and the exception-during-persistence path untested.

## Acceptance criteria

- [x] A handler returning a classified success commits the transaction/session
- [x] A handler returning a classified failure outcome rolls back the transaction/session
- [x] An exception during persistence rolls back and rethrows the original exception
- [x] Existing `IExternalStoreRequest` skip-path and generic-constraint tests still pass
- [x] Commit/rollback decisions match the EF Core behavior for identical classified inputs (both behaviors agree)

## Blocked by

None - can start immediately
