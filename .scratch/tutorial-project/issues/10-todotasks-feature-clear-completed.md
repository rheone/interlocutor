Status: open

## Labels

ready-for-agent

## What to build

`documentation/beginners-guide-todo-api.md` Step 9's "One more command: no payload, caller-chosen outcome type" sub-section, at `Features/Tasks/ClearCompletedTodoTasks/` (**not** `Features/TodoTasks/` - see ticket 05's Comments for why):

- **`ClearCompletedTodoTasksCommand`**: bare `ICommand` (no `<TResponse>`), `IAuditableRequest` - as written in the guide.
- **`ClearCompletedTodoTasksHandler`**: `IVoidCommandHandler<ClearCompletedTodoTasksCommand, OperationOutcome>` - deletes every completed task owned by the caller, one `SaveChangesAsync` - as written in the guide.
- Endpoint: `POST /todo-tasks/clear-completed`, authenticated, `sender.Send<OperationOutcome>(...)` since the response type is caller-chosen rather than fixed by the command - as written in the guide. A code comment should explain *why* this one needs the explicit type argument when every other endpoint in the project doesn't (the guide's own Step 9 prose already explains this - restate it briefly, don't just cite the guide).

## Acceptance criteria

- [ ] `ClearCompletedTodoTasksCommand`/`ClearCompletedTodoTasksHandler` exist, XML-documented, explain the bare-`ICommand`/caller-chosen-`TResult` mechanism in their own words
- [ ] Unit test: clears exactly the completed tasks for the given owner, leaves incomplete tasks and other owners' tasks untouched
- [ ] HTTP integration test: complete a couple of tasks (via ticket 08's endpoint), leave one incomplete, call clear-completed, then list and confirm only the incomplete one remains
- [ ] `dotnet build`, `dotnet test`, `dotnet csharpier .` all clean

## Blocked by

08
