Status: open

## Labels

ready-for-agent

## What to build

`documentation/beginners-guide-todo-api.md` Step 7, at `Features/Tasks/UpdateTodoTask/` (**not** `Features/TodoTasks/` - see ticket 05's Comments for why):

- **`UpdateTodoTaskCommand`**: `ICommand<OperationOutcome<TodoTaskDto, NotFound, Failure>>`, `IAuditableRequest` (`ActionName => "update_todo_task"`) - as written in the guide. `RequestedByUserId` filled in by the endpoint from the authenticated caller, never trusted from the request body.
- **`UpdateTodoTaskHandler`**: `ResourceCommandHandler<UpdateTodoTaskCommand, TodoTask, TodoTaskDto>` - load (404 if missing) → authorize against the *loaded* entity's `OwnerId` (`Failure` if the caller isn't the owner) → handle (apply the update, save) - as written in the guide. No FluentValidation on this command (the guide's own `[!IMPORTANT]` callout explains why: `ResourceCommandHandler`'s fixed `OperationOutcome<TSuccess, NotFound, Failure>` arity has no `ValidationErrors` case) - if input checks are wanted, fold them into `Authorize`/`HandleAsync` as the guide describes, don't register a validator.
- Endpoint: `PUT /todo-tasks/{id:int}`, authenticated - as written in the guide.
- This is the guide's contrast case against ticket 09's coarse `IRequiresPolicy` check - a code comment here should point at that contrast explicitly (`documentation/beginners-guide-todo-api.md` Step 7's own closing paragraph already states it; echo it briefly in the handler's XML doc remarks).

## Acceptance criteria

- [ ] `UpdateTodoTaskCommand`/`UpdateTodoTaskHandler` exist, XML-documented, explain the load/authorize/handle template and point at ticket 09's contrasting authorization style
- [ ] Unit tests: success, not-found, wrong-owner `Failure`
- [ ] HTTP integration test: the task's owner can update it; a different authenticated user gets a failure response; a missing task returns not-found
- [ ] `dotnet build`, `dotnet test`, `dotnet csharpier .` all clean

## Blocked by

05
