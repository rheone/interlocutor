# 13 - Users feature: Delete (multi-repo cascade)

**What to build:** Delete endpoint/command/handler for Users. Deleting a user loads and deletes every `TodoTask` they own via `IDomainRepository<TodoTask, ...>`, then deletes the user via `IDomainRepository<User, ...>`, with one `IUnitOfWork.SaveChangesAsync()` committing all of it atomically - this repo's own multi-repository unit-of-work worked example, made real rather than staying an illustrative aside.

**Blocked by:** 05

**Status:** done

- [x] Deleting a user also deletes every task they own, in one atomic commit
- [x] A failure partway through leaves neither the user nor their tasks deleted (proven, not assumed - a persistence test that forces a mid-flight failure and asserts nothing committed)
- [x] Unit and persistence tests cover the cascade

## Comments

**2026-09-04** - Implemented: `DeleteUserHandler` injects both `IDomainRepository<User, int>` and `IDomainRepository<TodoTask, int>` plus `IUnitOfWork` - loads and deletes every task owned by the user (string-matched against `TodoTask.OwnerId`, no FK), deletes the user, then one `SaveChangesAsync()` call flushes all of it. The load-bearing rollback proof (`DeleteUserThenFailHandler`/`DeleteUserThenFailCommand`, test-only twins of the real handler) runs the exact same cascade but forces a `Conflict` *after* the flush, driven directly through the real `UnitOfWorkBehavior<,,>` commit-gate - proving the DELETE statements that already executed against the real database get rolled back, not merely never sent.

**Bug found and fixed during verification**: the two straightforward success-path tests originally called `DeleteUserHandler.Handle()` directly without wrapping it in `UnitOfWorkBehavior`, so `EntityFrameworkUnitOfWork.SaveChangesAsync`'s transaction was flushed but never committed (commit happens at pipeline completion, not inside `SaveChangesAsync` itself) - the delete appeared to work only because the shared kept-open connection made the uncommitted write visible until the scope disposed and implicitly rolled it back, which a "brand new scope" assertion afterward correctly caught as a failure. Fixed by wrapping both tests' handler calls in `UnitOfWorkBehavior<DeleteUserCommand, Outcome<Success, NotFound>, TodoTasksDbContext>`, exactly like the rollback test already did correctly. Full solution build (0 errors) and test (1184/1184) green after the fix.
