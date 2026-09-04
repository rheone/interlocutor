# 11 - TodoTasks feature: Search (EF Core equivalent)

**What to build:** A case-insensitive title search over the caller's tasks. The original project's NHibernate build did this with QueryOver directly against the session, bypassing `IDomainRepository<TEntity, TKey>`, because the repository's fixed method set doesn't expose substring matching. This ticket has no NHibernate-specific mechanism to port - build the EF Core equivalent instead: drop to the `DbContext` directly (LINQ `Contains` with an appropriate case-insensitive comparison, or `EF.Functions`) for the same reason the original bypassed the repository.

**Blocked by:** 05

**Status:** done

- [x] Search returns owner-scoped, case-insensitive title matches
- [x] A code comment explains why this bypasses the repository, mirroring the original's rationale
- [x] Persistence and HTTP tests cover the search path

## Comments

**2026-09-04** - Implemented: `SearchTodoTasksHandler` injects `Lazy<TodoTasksDbContext>` directly rather than `IDomainRepository<TodoTask, int>`, with XML remarks mirroring the shelved NHibernate handler's rationale (the repository's fixed method set has no substring-match operation). Case-insensitive matching uses `EF.Functions.Like`, not LINQ `.Contains()` - verified against the real generated SQL that `Contains` translates to SQLite's case-sensitive `instr()`, while `LIKE` is case-insensitive for ASCII by default; a plain `Contains` would have compiled fine and silently failed the case-insensitivity requirement. Persistence tests prove case-insensitive matching, non-matching exclusion, and cross-owner exclusion against a real database; HTTP tests cover the authenticated end-to-end path. Full solution build (0 errors) and test (1169/1169) green.
