Status: done

## Labels

ready-for-agent

## What to build

`documentation/beginners-guide-todo-api.md` Steps 3-5, implemented exactly, at `Features/TodoTasks/CreateTodoTask/`:

- **`Domain/TodoTask.cs`** / **`Domain/TodoTaskDto.cs`**: as written in the guide's Step 3 (protected parameterless constructor, virtual members, `OwnerId` never settable from outside the constructor).
- **`Persistence/TodoTaskMap.cs`**: as written in the guide's Step 4.
- Repository registration: `AddInterlocutorUnitOfWork`/`AddInterlocutorRepository<TodoTask, int, ...>` in `Program.cs`, alongside the `User` registration from ticket 03 - both built over the same scoped `Lazy<ISession>`.
- **`CreateTodoTaskCommand`**: `ICommand<OperationOutcome<TodoTaskDto, ValidationErrors, Conflict>>`, `ITransactionalRequest`, `IAuditableRequest` (`ActionName => "create_todo_task"`) - as written in the guide.
- **`CreateTodoTaskValidator`**: FluentValidation, as written in the guide.
- **`CreateTodoTaskHandler`**: duplicate-title `Conflict` check, insert, `SaveChangesAsync` - as written in the guide.
- Endpoint: `POST /todo-tasks`, reads the authenticated caller's id via `IRequestContextAccessor` (throws if absent, per the guide's own stated rule that a missing authenticated caller is a bug, not an outcome), requires authorization (ticket 04's scheme) - as written in the guide's Step 5.

This is the first ticket where `IAuditSink`/`LoggingAuditSink` (registered in ticket 02) and `ITransactionalRequest`/`IAuditableRequest` actually do something observable - the acceptance criteria below should prove that, not just that the happy path returns 200.

Two corrections found the hard way during ticket 03, both apply here too:

- **Register `TodoTaskDto` with the outcome classifier.** Add `.RegisterSuccessType<TodoTaskDto>("TodoTaskDto")` to `Infrastructure/TodoTasksOutcomeDescriber.cs`'s `Classifier`. Skipping this makes `CreateTodoTaskHandler` return a genuine `TodoTaskDto` success while `UnitOfWorkBehavior` silently rolls back the insert underneath it - see ticket 03's own Comments and `TodoTasksOutcomeDescriber`'s remarks for the full account. The HTTP integration test below (create, then confirm it's actually retrievable) is what catches this if it's missed - a unit test alone won't.
- **`Conflict`'s constructor is `Conflict(string Message, string Code)`** - `Message` first, `Code` second. The guide's own `new Conflict("duplicate_title", $"You already have a task titled '{command.Title}'.")` has the arguments in the wrong order relative to the actual shipped record (confirmed against `src/Interlocutor.CommonKernel/ResultTypes/Conflict.cs`, not assumed from the guide's prose). Use named arguments - `new Conflict(Message: "...", Code: "duplicate_title")` - the same fix already applied in `CreateUserHandler`.

## Acceptance criteria

- [x] `TodoTask`/`TodoTaskDto`/`TodoTaskMap`/`CreateTodoTaskCommand`/`CreateTodoTaskValidator`/`CreateTodoTaskHandler` all exist, XML-documented, matching the guide
- [x] Unit tests for the handler: success, duplicate-title `Conflict`, validation failure (empty title, title/notes over length)
- [x] Persistence/mapping integration test proves `TodoTask` round-trips through the real in-memory SQLite mapping
- [x] HTTP integration test: authenticated `POST /todo-tasks` succeeds and returns the created task; unauthenticated request is rejected; duplicate title for the same owner returns a conflict response
- [x] Log output confirms `LoggingAuditSink` actually recorded the `"create_todo_task"` action (a log-capturing `ILoggerProvider` attached via `WebApplicationFactory.WithWebHostBuilder`, asserting on the real log line rather than eyeballing console output)
- [x] `dotnet build`, `dotnet test`, `dotnet csharpier .` all clean (860/860 solution-wide)

## Blocked by

04

## Comments

Two more findings beyond the two corrections already folded into "What to build" above:

1. **`Features/TodoTasks/` as a folder/namespace collides with the project's own root namespace `TodoTasks.Api`.** C#'s `using` resolution (searched inside a namespace block, per this repo's `usingDirectivesPlacement: insideNamespace` convention) walks outward through enclosing scopes and, once it finds *any* namespace matching a `using` directive's first segment, commits to it - it does not fall back to the global root if that match turns out to be a dead end. Once `TodoTasks.Api.Features.TodoTasks` existed as a real namespace, `using TodoTasks.Api.Domain;` written in *any* file under `TodoTasks.Api.Features.*` (not just the `TodoTasks` subfolder itself - `Features/Users/*` broke too) started resolving `TodoTasks` against the sibling `Features.TodoTasks` segment instead of the project's own root, failing to compile. Renamed the feature-area folder to `Features/Tasks/` (namespace `TodoTasks.Api.Features.Tasks`) to remove the collision at its source rather than requiring `global::`-qualified usings everywhere under `Features/**` forever - a one-time fix instead of an ongoing footgun. **Tickets 06-10's own "What to build" sections below still say `Features/TodoTasks/...` in places - read that as `Features/Tasks/...`.**
2. **SQLite's NHibernate dialect has no mapping for `DbType.DateTimeOffset` at all** - mapping `TodoTask.CreatedAtUtc` (a `DateTimeOffset`) the guide's own straightforward way throws `ArgumentException: Dialect does not support DbType.DateTimeOffset` the moment the schema exports (confirmed against the real exception, not assumed - SQLite has no native timestamp-with-offset column type, unlike SQL Server, which is what the guide targets). Added `Persistence/DateTimeOffsetAsStringUserType.cs`, a small `IUserType` storing the value as a round-trippable (`"o"` format) string, applied via `.CustomType<DateTimeOffsetAsStringUserType>()` on the mapping. Its first draft had a separate bug worth flagging for whoever writes the next custom `IUserType` in this project: an unqualified `Equals(x, y)` call inside `IUserType.Equals(object,object)` resolves back to that very same method once the arguments box to `object`, recursing until the stack overflows (reproduced directly, not hypothetical) - call `object.Equals(x, y)` explicitly instead. Any future entity with a `DateTimeOffset` column should reuse this same type.
