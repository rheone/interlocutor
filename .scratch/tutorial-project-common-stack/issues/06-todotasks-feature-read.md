# 06 - TodoTasks feature: Read (GetById + List)

**What to build:** GetById and List endpoints/queries/handlers for TodoTasks, owner-scoped to the authenticated caller.

**Blocked by:** 05

**Status:** done

- [x] `GET` by id returns the task if owned by the caller, not-found otherwise (including when it exists but is owned by someone else)
- [x] `GET` list returns only the caller's own tasks
- [x] Unit, persistence, and HTTP tests cover both endpoints

## Comments

**2026-09-04** - Implemented: `GetTodoTaskByIdHandler` returns the same `NotFound` for a nonexistent id and for a task that exists but belongs to a different owner, so existence is never leaked to a non-owner. `ListTodoTasksHandler` filters by the authenticated caller's `OwnerId`. Both endpoints require authorization. Unit (mocked), persistence (real SQLite), and HTTP tests cover both endpoints including the cross-owner-NotFound and list-scoping cases explicitly. Found and fixed a real EF Core/SQLite bug along the way: sorting by `CreatedAtUtc` (a `DateTimeOffset`) at the repository level throws `NotSupportedException` under the SQLite provider (no native `DateTimeOffset` type to `ORDER BY` on) - invisible to mocked unit tests, caught by the persistence tests. Fixed by sorting client-side after materializing instead of pushing the sort to the query. Full solution build (0 errors) and test (1109/1109) green.
