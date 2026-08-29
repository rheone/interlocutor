Status: done

## Labels

ready-for-agent

## What to build

`documentation/beginners-guide-todo-api.md` Step 9's "One more command: no payload, caller-chosen outcome type" sub-section, at `Features/Tasks/ClearCompletedTodoTasks/` (**not** `Features/TodoTasks/` - see ticket 05's Comments for why):

- **`ClearCompletedTodoTasksCommand`**: bare `ICommand` (no `<TResponse>`), `IAuditableRequest` - as written in the guide.
- **`ClearCompletedTodoTasksHandler`**: `IVoidCommandHandler<ClearCompletedTodoTasksCommand, OperationOutcome>` - deletes every completed task owned by the caller, one `SaveChangesAsync` - as written in the guide.
- Endpoint: `POST /todo-tasks/clear-completed`, authenticated, `sender.Send<OperationOutcome>(...)` since the response type is caller-chosen rather than fixed by the command - as written in the guide. A code comment should explain *why* this one needs the explicit type argument when every other endpoint in the project doesn't (the guide's own Step 9 prose already explains this - restate it briefly, don't just cite the guide).

## Acceptance criteria

- [x] `ClearCompletedTodoTasksCommand`/`ClearCompletedTodoTasksHandler` exist, XML-documented, explain the bare-`ICommand`/caller-chosen-`TResult` mechanism in their own words
- [x] Unit tests: deletes exactly the completed tasks returned by the repository (one `SaveChangesAsync`); no-op when none are completed; the filter expression passed to the repository is proven (by compiling and invoking it) to require both the right owner *and* `IsCompleted`
- [x] HTTP integration test: complete one task, leave another incomplete, call clear-completed, confirm the completed one is gone (404) and the incomplete one remains (200); a second test confirms unauthenticated calls are rejected
- [x] `dotnet build`, `dotnet test`, `dotnet csharpier .` all clean (901/901 solution-wide)

## Blocked by

08

## Comments

`Success` (this handler's only real outcome case) maps to HTTP `204 No Content`, not `200 OK` - my first draft of the HTTP test assumed `200` and failed for that reason alone, not a handler bug. Worth remembering for any future test asserting on a `Success`-only response.
