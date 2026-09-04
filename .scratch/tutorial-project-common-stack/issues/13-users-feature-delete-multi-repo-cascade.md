# 13 - Users feature: Delete (multi-repo cascade)

**What to build:** Delete endpoint/command/handler for Users. Deleting a user loads and deletes every `TodoTask` they own via `IDomainRepository<TodoTask, ...>`, then deletes the user via `IDomainRepository<User, ...>`, with one `IUnitOfWork.SaveChangesAsync()` committing all of it atomically - this repo's own multi-repository unit-of-work worked example, made real rather than staying an illustrative aside.

**Blocked by:** 05

**Status:** ready-for-agent

- [ ] Deleting a user also deletes every task they own, in one atomic commit
- [ ] A failure partway through leaves neither the user nor their tasks deleted (proven, not assumed - a persistence test that forces a mid-flight failure and asserts nothing committed)
- [ ] Unit and persistence tests cover the cascade
