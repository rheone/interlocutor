# 10 - TodoTasks feature: ClearCompleted

**What to build:** A bulk-clear endpoint/command/handler that deletes every completed task owned by the caller.

**Blocked by:** 08

**Status:** done

- [x] Clearing removes all of the caller's completed tasks and none of their incomplete ones, and none of another user's tasks
- [x] Unit and persistence tests cover the bulk path

## Comments

**2026-09-04** - Implemented: `ClearCompletedTodoTasksCommand` is the bare `ICommand` marker (caller-chosen `TResult`), matching the guide's "no payload, caller-chosen outcome type" section; `ClearCompletedTodoTasksHandler` is an `IVoidCommandHandler<ClearCompletedTodoTasksCommand, Outcome>`, deletes every completed task for the caller's `OwnerId`, and the endpoint calls `sender.Send<Outcome>(...)` - matching the shelved project's and the guide's own precedent (a richer count-reporting `Outcome<int, Failure>` is explicitly framed there as a hypothetical alternative, not what this handler needs). Unit tests (mocked repository) prove the filter excludes the caller's incomplete tasks and another owner's tasks; persistence tests seed two owners each with completed/incomplete tasks and prove only the caller's completed rows are gone. Full solution build (0 errors) and test (1164/1164) green.
