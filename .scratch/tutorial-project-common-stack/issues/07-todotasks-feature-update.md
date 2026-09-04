# 07 - TodoTasks feature: Update

**What to build:** Update endpoint/command/handler for TodoTasks, owner-scoped.

**Blocked by:** 05

**Status:** done

- [x] Updating an owned task succeeds and persists
- [x] Updating a non-owned or nonexistent task fails with the appropriate outcome, not a generic `Failure`
- [x] Unit and persistence tests cover both paths

## Comments

**2026-09-04** - Implemented: `UpdateTodoTaskHandler` uses `ResourceCommandHandler<UpdateTodoTaskCommand, TodoTask, TodoTaskDto>`, matching the guide's Step 7 and the shelved project - fixed response shape `Outcome<TodoTaskDto, NotFound, Forbidden>`. A non-owned task maps to `Forbidden` (existence already established by the base class's `LoadAsync` before `Authorize` runs, so nothing is leaked, unlike ticket 06's Get where folding both cases into `NotFound` is what prevents the leak); a nonexistent task maps to `NotFound`. Unit, persistence (real SQLite, cross-scope), and HTTP tests cover both paths at every layer. Full solution build (0 errors) and test (1119/1119) green.
