Status: open

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

- [ ] `TodoTask`/`TodoTaskDto`/`TodoTaskMap`/`CreateTodoTaskCommand`/`CreateTodoTaskValidator`/`CreateTodoTaskHandler` all exist, XML-documented, matching the guide
- [ ] Unit tests for the handler: success, duplicate-title `Conflict`, validation failure (empty title, title/notes over length)
- [ ] Persistence/mapping integration test proves `TodoTask` round-trips through the real in-memory SQLite mapping
- [ ] HTTP integration test: authenticated `POST /todo-tasks` succeeds and returns the created task; unauthenticated request is rejected; duplicate title for the same owner returns a conflict response; the duplicate-title check and the insert are proven to share one transaction (mirroring `SharedSessionIntegrationTests`' pattern in `tests/Interlocutor.Persistence.NHibernate.Tests`)
- [ ] Log output (or a test double for `IAuditSink`) confirms `LoggingAuditSink` actually recorded the `"create_todo_task"` action
- [ ] `dotnet build`, `dotnet test`, `dotnet csharpier .` all clean

## Blocked by

04
