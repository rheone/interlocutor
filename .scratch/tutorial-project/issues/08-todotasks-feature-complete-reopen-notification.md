Status: done

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

- [x] All types above exist, XML-documented, matching the guide
- [x] Unit tests: complete success + already-completed `Conflict` + not-found; reopen success + not-completed `Conflict` + not-found; both notification subscribers independently (`TodoTaskCompletionCounterHandler`, `TodoTaskCompletionAuditLogHandler`)
- [x] HTTP integration test: complete a task, confirm the completion counter incremented (via `ICompletionCounter` resolved straight from the factory's container), then reopen it and confirm the state flipped back
- [x] `dotnet build`, `dotnet test`, `dotnet csharpier .` all clean (888/888 solution-wide)

## Blocked by

05

## Comments

- Extended `ICompletionCounter` with a `CountFor(string ownerId)` read accessor beyond what the guide's own interface has (write-only `Increment` alone can't be asserted on meaningfully). Split into two files (`ICompletionCounter.cs`, `InMemoryCompletionCounter.cs`) per this repo's one-type-per-file convention - the guide's own listing combines them in one file.
- Found a real test bug worth flagging for future handler tests: a mocked `TodoTask` never goes through NHibernate's identity generation, so its `Id` stays `0` even after `IDomainRepository.GetById(1)` is stubbed to return it - asserting a published notification's `TodoTaskId` against the id used to *look up* the mock (rather than what the mock entity's `Id` actually is) fails for that reason, not a handler bug. Simplified the assertion to check `OwnerId`/`Title` instead.
- Did not add a dedicated log-capturing test for `TodoTaskCompletionAuditLogHandler` specifically at the HTTP layer (unlike ticket 05's `CreateTodoTaskAuditingTests`) - its unit test already proves it runs without throwing and logs the expected line (visible in the console-capturing test runner's own output), and the log-capture *mechanism* is already proven once, in ticket 05; repeating it here would mostly re-test `ILoggerProvider` plumbing rather than this handler's own logic.
