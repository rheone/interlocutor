## Labels

ready-for-agent

## What to build

Write tests for the transactional flow of `TransactionBehavior` in both `Mediance.EntityFrameworkCore` and `Mediance.NHibernate`.

Currently, both transaction behaviors only have a single test for the non-transactional skip path (request not implementing `ITransactionalRequest`). The actual transactional flow — commit on success, rollback on failure — is **untested**.

### EF Core tests needed:
1. **Transactional request commits on success**: Request implements `ITransactionalRequest`, next() returns success, verify `SaveChangesAsync` and `CommitAsync` were called
2. **Transactional request rolls back on Failure**: Request implements `ITransactionalRequest`, next() returns `Failure`, verify `RollbackAsync` was called (not Commit)
3. **SaveChangesAsync failure triggers explicit rollback**: next() succeeds but SaveChangesAsync throws, verify rollback was called before exception propagates
4. **IExternalStoreRequest skips transaction**: Request implements both `ITransactionalRequest` and `IExternalStoreRequest`, verify transaction is not created and Lazy<DbContext> is not resolved (depends on ticket 07)

### NHibernate tests needed:
Same scenarios 1-3 using NHibernate `ISession` and `ITransaction`.

Use in-memory EF Core provider and SQLite/NHibernate in-memory for isolation.

## Acceptance criteria

- [ ] EF Core TransactionBehavior commit path tested
- [ ] EF Core TransactionBehavior rollback path tested
- [ ] EF Core SaveChangesAsync failure path tested
- [ ] EF Core IExternalStoreRequest skip tested
- [ ] NHibernate TransactionBehavior commit/rollback/failure paths tested
- [ ] All existing tests still pass

## Blocked by

- 07-ef-core-transaction-behavior (adds IExternalStoreRequest and error handling)
