Status: open

## Labels

ready-for-agent

## What to build

`documentation/beginners-guide-todo-api.md` Step 8's core (not the "Bonus: forcing a rollback" or "Bonus: multiple repositories" sub-sections - the latter is superseded by ticket 11's real cascade-delete example instead of an illustrative activity log; the former is backlog ticket 16), at `Features/Tasks/CompleteTodoTask/` and `Features/Tasks/ReopenTodoTask/` (**not** `Features/TodoTasks/` - see ticket 05's Comments for why):

- **`TodoTaskCompletedNotification`**: `INotification`, as written in the guide.
- **`CompleteTodoTaskCommand`**/handler: `ICommand<OperationOutcome<TodoTaskDto, NotFound, Conflict>>`, `IAuditableRequest` - not-found, already-completed `Conflict`, else mark complete, save, then `publisher.Publish(...)` (persist-then-publish ordering, as the guide states and as its own code comment explains). `POST /todo-tasks/{id:int}/complete`.
- **`ReopenTodoTaskCommand`**/handler: the mirror image (`Conflict` when *not* completed, no notification) - as written in the guide. `POST /todo-tasks/{id:int}/reopen`.
- **`Infrastructure/ICompletionCounter.cs`** + `InMemoryCompletionCounter`: as written in the guide.
- **`TodoTaskCompletionCounterHandler`** and **`TodoTaskCompletionAuditLogHandler`**: two independent `INotificationHandler<TodoTaskCompletedNotification>` subscribers, as written in the guide - the test suite should prove both ran off one `Publish` call, independently of each other.

## Acceptance criteria

- [ ] All types above exist, XML-documented, matching the guide
- [ ] Unit tests: complete success + already-completed `Conflict` + not-found; reopen success + not-completed `Conflict` + not-found; both notification subscribers independently, and together off one `Publish`
- [ ] HTTP integration test: complete a task, confirm the completion counter incremented (via `ICompletionCounter`) and the audit log entry was written (via log capture or a test double), then reopen it and confirm the state flipped back with no new notification firing
- [ ] `dotnet build`, `dotnet test`, `dotnet csharpier .` all clean

## Blocked by

05
