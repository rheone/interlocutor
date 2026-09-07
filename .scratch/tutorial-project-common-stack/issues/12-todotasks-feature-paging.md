# 12 - TodoTasks feature: Paging

**What to build:** A paged variant of the TodoTasks list endpoint, using this repo's own `Page<T>`/paging conventions (see `documentation/persistence-abstraction.md`).

**Blocked by:** 06

**Status:** done

- [x] The list endpoint accepts paging parameters and returns a `Page<T>`-shaped result with correct total counts
- [x] Persistence and HTTP tests cover at least a partial page, an empty page, and a boundary page

## Comments

**2026-09-04** - Implemented: `GET /todo-tasks/paged?page=&pageSize=`, owner-scoped, via `IDomainRepository<TodoTask,int>.GetPagedAsync`, mapping `Page<TodoTask>` to `PagedTodoTasksDto` (Items/Page/PageSize/TotalCount). Sort key is `Id` descending, not `CreatedAtUtc` - paging's `Skip`/`Take` must order at the database level (unlike the unpaged list/search handlers, which sort client-side specifically because SQLite can't translate `ORDER BY` over `DateTimeOffset`), and `Id` increases in creation order so gives the same newest-first result while translating to real SQL (verified in the EF Core command log). Persistence tests cover partial, empty (beyond available data), and boundary (two exactly-full pages) cases against a real database; HTTP tests cover the authenticated path. Full solution build (0 errors) and test (1177/1177) green.
