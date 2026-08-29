Status: done

## Labels

ready-for-agent

## What to build

`documentation/beginners-guide-todo-api.md` Step 7, at `Features/Tasks/UpdateTodoTask/` (**not** `Features/TodoTasks/` - see ticket 05's Comments for why):

- **`UpdateTodoTaskCommand`**: `ICommand<OperationOutcome<TodoTaskDto, NotFound, Failure>>`, `IAuditableRequest` (`ActionName => "update_todo_task"`) - as written in the guide. `RequestedByUserId` filled in by the endpoint from the authenticated caller, never trusted from the request body.
- **`UpdateTodoTaskHandler`**: `ResourceCommandHandler<UpdateTodoTaskCommand, TodoTask, TodoTaskDto>` - load (404 if missing) → authorize against the *loaded* entity's `OwnerId` (`Failure` if the caller isn't the owner) → handle (apply the update, save) - as written in the guide. No FluentValidation on this command (the guide's own `[!IMPORTANT]` callout explains why: `ResourceCommandHandler`'s fixed `OperationOutcome<TSuccess, NotFound, Failure>` arity has no `ValidationErrors` case) - if input checks are wanted, fold them into `Authorize`/`HandleAsync` as the guide describes, don't register a validator.
- Endpoint: `PUT /todo-tasks/{id:int}`, authenticated - as written in the guide.
- This is the guide's contrast case against ticket 09's coarse `IRequiresPolicy` check - a code comment here should point at that contrast explicitly (`documentation/beginners-guide-todo-api.md` Step 7's own closing paragraph already states it; echo it briefly in the handler's XML doc remarks).

## Acceptance criteria

- [x] `UpdateTodoTaskCommand`/`UpdateTodoTaskHandler` exist, XML-documented, explain the load/authorize/handle template and point at ticket 09's contrasting authorization style
- [x] Unit tests: success, not-found, wrong-owner `Failure`
- [x] HTTP integration test: the task's owner can update it; a different authenticated user gets a failure response; a missing task returns not-found
- [x] `dotnet build`, `dotnet test`, `dotnet csharpier .` all clean (875/875 solution-wide)

## Blocked by

05

## Comments

**Third guide-vs-shipped-library gap found** (after ticket 03's classifier registration and ticket 05's `Conflict` argument order): `ResourceCommandHandler<TCommand,TEntity,TSuccess>` does **not** implement `ICommandHandler<TCommand,TResponse>` (confirmed against the shipped source in `src/Interlocutor.Abstractions/Commands/ResourceCommandHandler.cs`, not assumed) - subclassing it alone, exactly as the guide's own Step 7 example shows, is invisible to `ScanAssembly`'s handler discovery. Every dispatch to `UpdateTodoTaskCommand` threw `System.InvalidOperationException: The requested service '...ICommandHandler<UpdateTodoTaskCommand, ...>' has not been registered` until `UpdateTodoTaskHandler`'s own class declaration explicitly added `, ICommandHandler<UpdateTodoTaskCommand, OperationOutcome<TodoTaskDto, NotFound, Failure>>` alongside the `ResourceCommandHandler<...>` base - the base class's own public `Handle` method already matches that interface's signature exactly, so no override body is needed, just the declaration. Documented in the handler's own remarks. This project has only the one `ResourceCommandHandler` subclass, so no other ticket needs this note - but if a future advanced-feature ticket adds another one, it needs the same explicit interface declaration.
