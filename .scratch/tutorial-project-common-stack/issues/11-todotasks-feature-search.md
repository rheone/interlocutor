# 11 - TodoTasks feature: Search (EF Core equivalent)

**What to build:** A case-insensitive title search over the caller's tasks. The original project's NHibernate build did this with QueryOver directly against the session, bypassing `IDomainRepository<TEntity, TKey>`, because the repository's fixed method set doesn't expose substring matching. This ticket has no NHibernate-specific mechanism to port - build the EF Core equivalent instead: drop to the `DbContext` directly (LINQ `Contains` with an appropriate case-insensitive comparison, or `EF.Functions`) for the same reason the original bypassed the repository.

**Blocked by:** 05

**Status:** ready-for-agent

- [ ] Search returns owner-scoped, case-insensitive title matches
- [ ] A code comment explains why this bypasses the repository, mirroring the original's rationale
- [ ] Persistence and HTTP tests cover the search path
