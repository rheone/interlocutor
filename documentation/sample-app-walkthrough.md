# Sample App Walkthrough

A guided tour of the runnable project under [`TutorialProject/TodoTasks.Api`](../TutorialProject/TodoTasks.Api/) - a minimal API project that *is* [the Beginner's Guide](beginners-guide-todo-api.md), every "blank" the guide leaves as an exercise filled in: NHibernate against an in-memory SQLite database, FluentValidation, Autofac, one command/query/handler/endpoint set per use case. A pair of MVC controllers sit alongside that minimal-API surface as an alternate transport for the same commands/queries/handlers - see "Transport conversion at the edge" below. It's a proof-of-concept, excluded from packaging (`IsPackable=false`) and never released - it exists to be read, run, and tested, not shipped.

If you haven't read [Glossary](glossary.md), [Messages, Result Types & Variance](messages-and-result-types.md), and [Builder & Registration](builder-and-registration.md) yet, do that first - this walkthrough assumes the vocabulary and API shape those establish and shows them applied together rather than re-explaining them.

## What it is

A to-do list API with two features: **Tasks** (create, look up by id, list, update, complete, reopen, delete, clear completed) and **Users** (create, look up by id, delete). Every piece maps to a piece of Interlocutor's design:

| Project piece | What it demonstrates |
| --- | --- |
| `GetTodoTaskByIdQuery` / `GetTodoTaskByIdHandler` | A plain query handler - read-only, never transactional |
| `CreateTodoTaskCommand` / `CreateTodoTaskHandler` | A value-returning command handler, with `ITransactionalRequest` protecting a duplicate-title check against a concurrent insert |
| `DeleteTodoTaskCommand` | `IRequiresPolicy`, coarse-grained authorization evaluated before the entity ever loads |
| `UpdateTodoTaskCommand` / `UpdateTodoTaskHandler` | `ResourceCommandHandler`'s load/authorize/handle template - fine-grained, entity-based authorization |
| `DeleteUserHandler` | One `IUnitOfWork` spanning two repositories - a user and every task they own deleted together or not at all |
| `TodoTaskCompletedNotification` with two handlers | A multi-subscriber notification |
| `DevHeaderAuthenticationHandler` | A minimal `AuthenticationHandler` standing in for real authentication, backed by the Users feature's own repository |
| `ToMinimalApiResult()` at each endpoint | Transport conversion at the edge |
| `TodoTasksController`, `UsersController` | An MVC-controller transport for the same handlers, exposed under `/api/`; `TodoTasksController.GetById` pattern-matches the outcome's cases directly instead of calling `ToActionResult()`, a real, compiler-checked example of consuming the union by hand |

Unlike an earlier sample this project replaced, it doesn't demonstrate cross-assembly handler discovery (everything lives in one project) or a hand-composed pipeline (`Program.cs` calls `UseRecommendedChain()` - see [Pipeline & Behaviors](pipeline-and-behaviors.md) for what that chain is) or a pipeline stage that short-circuits (the duplicate-title check here is an ordinary read inside the handler, guarded by `ITransactionalRequest` instead of a `PipelineStageBehavior`-hosted stage). Describing only what this project actually does, rather than carrying those claims forward, is deliberate.

## Project structure

```
TutorialProject/TodoTasks.Api/
├── Domain/                    # TodoTask/User entities and their Dto projections
├── Persistence/                # Fluent NHibernate ClassMaps, the session factory, a custom IUserType
├── Infrastructure/             # dev auth handler, policy evaluator, outcome describer, completion counter
├── Features/
│   ├── Tasks/                  # one folder per use case: CreateTodoTask, GetTodoTaskById, ListTodoTasks,
│   │   │                       # UpdateTodoTask, CompleteTodoTask, ReopenTodoTask, DeleteTodoTask,
│   │   │                       # ClearCompletedTodoTasks
│   │   └── TodoTasksController.cs  # MVC-controller transport for the whole domain, under /api/todo-tasks
│   └── Users/                  # CreateUser, GetUserById, DeleteUser
│       └── UsersController.cs  # MVC-controller transport for the whole domain, under /api/users
└── Program.cs                  # Autofac host, DI wiring, one MapXEndpoint() call per feature, MapControllers()
```

This is vertical-slice structure **by directory within one project**, not one project per slice: every file a use case needs - its command or query, validator (if any), handler, and endpoint mapping - lives together in one folder, e.g. `Features/Tasks/CreateTodoTask/`. `Domain/` and `Persistence/` are the only things shared across slices, because an entity and its mapping genuinely belong to more than one use case at once. `TodoTasksController`/`UsersController` are the one deliberate exception to "one folder per use case": a controller spans every use case in its domain, so each lives at its domain's root instead of inside a single use-case folder - the same way `Program.cs` itself aggregates across use cases.

`Features/Tasks/` is deliberately not named `Features/TodoTasks/` - a subfolder segment matching the project's own root namespace (`TodoTasks.Api`) breaks C#'s `using`-resolution for every file under it, since a `using` directive inside a namespace block resolves outward and commits to the first matching segment it finds.

## Query handler example (read request, no transaction)

[`GetTodoTaskByIdQuery`](../TutorialProject/TodoTasks.Api/Features/Tasks/GetTodoTaskById/GetTodoTaskByIdQuery.cs) and its handler are the plainest possible shape - a record implementing `IQuery<TResponse>`, a handler implementing `IQueryHandler<TRequest, TResponse>`, no marker interfaces:

```csharp
public sealed record GetTodoTaskByIdQuery(int Id) : IQuery<Outcome<TodoTaskDto, NotFound>>;

public sealed class GetTodoTaskByIdHandler(IDomainRepository<TodoTask, int> tasks)
    : IQueryHandler<GetTodoTaskByIdQuery, Outcome<TodoTaskDto, NotFound>>
{
    public Task<Outcome<TodoTaskDto, NotFound>> Handle(GetTodoTaskByIdQuery query, CancellationToken ct) =>
        Task.FromResult<Outcome<TodoTaskDto, NotFound>>(
            tasks.GetById(query.Id) is { } task ? TodoTaskDto.FromEntity(task) : new NotFound($"TodoTask {query.Id}")
        );
}
```

Two cases, `TodoTaskDto` or `NotFound` - the query never writes, so `UnitOfWorkBehavior` never opens a transaction for it. A read has nothing to commit or roll back, and no marker is needed to say so.

## Command handler example (write request, transaction)

[`CreateTodoTaskCommand`](../TutorialProject/TodoTasks.Api/Features/Tasks/CreateTodoTask/CreateTodoTaskCommand.cs) is a value-returning command - it mutates state and hands back the entity it created, not just a bare outcome:

```csharp
public sealed record CreateTodoTaskCommand(string Title, string? Notes, string OwnerId)
    : ICommand<Outcome<TodoTaskDto, ValidationErrors, Conflict>>,
        ITransactionalRequest,
        IAuditableRequest
{
    public string ActionName => "create_todo_task";
}
```

Three cases: `TodoTaskDto` on success, `ValidationErrors` from [`CreateTodoTaskValidator`](../TutorialProject/TodoTasks.Api/Features/Tasks/CreateTodoTask/CreateTodoTaskValidator.cs) for an empty or overlong title, `Conflict` for a title the caller already has. `CreateTodoTaskHandler` checks the duplicate itself, as an ordinary read at the top of `Handle` - `ITransactionalRequest` is what keeps that read and the insert that follows it inside one transaction, eagerly begun at pipeline start, so two concurrent requests for the same title can't both pass the check before either commits. `IAuditableRequest`'s `ActionName` records the action via `AuditingBehavior` (part of `UseRecommendedChain()`). The handler never calls `Commit` itself - that's `UnitOfWorkBehavior`'s job, once the handler returns a success outcome.

## Fine-grained vs. coarse-grained authorization

[`DeleteTodoTaskCommand`](../TutorialProject/TodoTasks.Api/Features/Tasks/DeleteTodoTask/DeleteTodoTaskCommand.cs) uses `IRequiresPolicy` - `AuthorizationBehavior` evaluates its `PolicyName` via [`TodoTaskPolicyEvaluator`](../TutorialProject/TodoTasks.Api/Infrastructure/TodoTaskPolicyEvaluator.cs) *before* the task is ever loaded: "can this caller attempt this kind of command at all," not "is this specific task theirs."

[`UpdateTodoTaskCommand`](../TutorialProject/TodoTasks.Api/Features/Tasks/UpdateTodoTask/UpdateTodoTaskCommand.cs) needs the opposite: only the task's *owner* may update it, a check that can only run after the task is loaded. [`UpdateTodoTaskHandler`](../TutorialProject/TodoTasks.Api/Features/Tasks/UpdateTodoTask/UpdateTodoTaskHandler.cs) uses `ResourceCommandHandler`'s load/authorize/handle template instead:

```csharp
public sealed class UpdateTodoTaskHandler(IDomainRepository<TodoTask, int> tasks, IUnitOfWork unitOfWork)
    : ResourceCommandHandler<UpdateTodoTaskCommand, TodoTask, TodoTaskDto>,
        ICommandHandler<UpdateTodoTaskCommand, Outcome<TodoTaskDto, NotFound, Failure>>
{
    protected override Task<TodoTask?> LoadAsync(UpdateTodoTaskCommand command, CancellationToken ct) =>
        Task.FromResult(tasks.GetById(command.Id));

    protected override Failure? Authorize(TodoTask entity, UpdateTodoTaskCommand command) =>
        entity.OwnerId == command.RequestedByUserId ? null : new Failure("Only the task's owner can update it.");

    protected override async Task<Outcome<TodoTaskDto, NotFound, Failure>> HandleAsync(
        UpdateTodoTaskCommand command, TodoTask entity, CancellationToken ct)
    {
        entity.Title = command.Title;
        entity.Notes = command.Notes;
        await unitOfWork.SaveChangesAsync(ct);
        return TodoTaskDto.FromEntity(entity);
    }
}
```

> [!IMPORTANT]
> `ResourceCommandHandler<TCommand, TEntity, TSuccess>` does not itself implement `ICommandHandler<TCommand, TResponse>` - a plain subclass is invisible to handler discovery. The explicit interface declaration above is required, not decorative; the base class's own `Handle` method already satisfies it, so no override body is needed.

`LoadAsync` returning `null` short-circuits with `NotFound` before `Authorize` ever runs; `Authorize` returning non-null short-circuits with that `Failure` before `HandleAsync` ever runs. Neither `NotFound` nor the ownership `Failure` reaches `HandleAsync` - it only ever sees an entity that was both found and authorized.

## Multi-repository unit-of-work example

[`DeleteUserHandler`](../TutorialProject/TodoTasks.Api/Features/Users/DeleteUser/DeleteUserHandler.cs) is this project's worked example of one `IUnitOfWork` spanning two repositories - deleting a user and every task they own, together or not at all:

```csharp
public sealed class DeleteUserHandler(
    IDomainRepository<User, int> users,
    IDomainRepository<TodoTask, int> tasks,
    IUnitOfWork unitOfWork
) : ICommandHandler<DeleteUserCommand, Outcome<Success, NotFound>>
{
    public async Task<Outcome<Success, NotFound>> Handle(DeleteUserCommand command, CancellationToken ct)
    {
        if (users.GetById(command.Id) is not { } user)
        {
            return new NotFound($"User {command.Id}");
        }

        var ownedTasks = await tasks.GetManyAsync(
            t => t.OwnerId == user.Id.ToString(CultureInfo.InvariantCulture),
            cancellationToken: ct
        );

        foreach (var task in ownedTasks)
        {
            await tasks.DeleteAsync(task, ct);
        }

        await users.DeleteAsync(user, ct);
        await unitOfWork.SaveChangesAsync(ct); // One commit for both repositories' staged deletes.

        return default(Success);
    }
}
```

Both repositories are built over the same scoped NHibernate session `IUnitOfWork` commits, so this one `SaveChangesAsync` call is what actually makes the deletion atomic - a task deletion that succeeded while the user deletion failed (or vice versa) is not a state this handler can leave behind. `TodoTask.OwnerId` stays an opaque string rather than a database foreign key to `User.Id` (see [ASP.NET Core Integration & HTTP Mapping](aspnetcore-integration.md) for how that string arrives from an authenticated caller in the first place), so the owned-tasks lookup is a string comparison, not a relational join.

## Multi-subscriber notification

[`CompleteTodoTaskHandler`](../TutorialProject/TodoTasks.Api/Features/Tasks/CompleteTodoTask/CompleteTodoTaskHandler.cs) publishes [`TodoTaskCompletedNotification`](../TutorialProject/TodoTasks.Api/Features/Tasks/CompleteTodoTask/TodoTaskCompletedNotification.cs) after marking a task complete:

```csharp
task.IsCompleted = true;
await unitOfWork.SaveChangesAsync(ct);

await publisher.Publish(new TodoTaskCompletedNotification(task.Id, task.OwnerId, task.Title), ct);
```

Two independent handlers subscribe to it, neither aware of the other: [`TodoTaskCompletionCounterHandler`](../TutorialProject/TodoTasks.Api/Features/Tasks/CompleteTodoTask/TodoTaskCompletionCounterHandler.cs) increments an in-process per-owner completion count, and [`TodoTaskCompletionAuditLogHandler`](../TutorialProject/TodoTasks.Api/Features/Tasks/CompleteTodoTask/TodoTaskCompletionAuditLogHandler.cs) writes a log entry. See [Notifications & Fan-out](notifications-and-fan-out.md) for execution order guarantees (unspecified) and the Sequential/Parallel publish strategies, and [Transactions & Store Integration](transactions-and-store-integration.md)'s "Transactions with Notifications" section for how a command's transaction and its published notification relate.

## Development authentication

The guide's own "Before you start" section assumes real authentication middleware already exists and deliberately doesn't teach it. This project needs *something* to populate an authenticated caller, so [`DevHeaderAuthenticationHandler`](../TutorialProject/TodoTasks.Api/Infrastructure/DevHeaderAuthenticationHandler.cs) fills that gap - never a pattern to carry into a real application:

```csharp
protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
{
    if (!this.Request.Headers.TryGetValue(HeaderName, out var headerValues)
        || !int.TryParse(headerValues.ToString(), out var userId))
    {
        return AuthenticateResult.NoResult();
    }

    if (users.GetById(userId) is not { } user)
    {
        return AuthenticateResult.NoResult();
    }

    var identity = new ClaimsIdentity([new Claim(ClaimTypes.Name, user.Id.ToString(CultureInfo.InvariantCulture))], this.Scheme.Name);
    return await Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), this.Scheme.Name)));
}
```

It trusts an `X-User-Id` request header as the caller's claimed identity, but only authenticates it once it matches a real `User` row - so create a user first, then act as them by sending that user's id back on every later request. Ties the Tasks and Users features together: `Interlocutor.AspNetCore`'s `AspNetCoreRequestContextAccessor` reads the matched user's id off `HttpContext.User` exactly as the guide describes, nothing custom needed on Interlocutor's side.

## Transport conversion at the edge (ASP.NET Core integration)

Every endpoint calls `ToMinimalApiResult()` as its last step, converting the outcome's active case to an `IResult`:

```csharp
app.MapGet(
    "/todo-tasks/{id:int}",
    async (int id, ISender sender, CancellationToken ct) =>
        (await sender.Send(new GetTodoTaskByIdQuery(id), ct)).ToMinimalApiResult()
);
```

Handlers never see `IResult`, `HttpContext`, or any other ASP.NET Core type - the mapping happens once, at the endpoint delegate. See [ASP.NET Core Integration & HTTP Mapping](aspnetcore-integration.md) for the case-to-status-code mapping this extension applies.

`TodoTasksController` and `UsersController` expose the same commands/queries/handlers again, under `/api/todo-tasks` and `/api/users` - a second transport living alongside the minimal-API one (the two can't share routes; ASP.NET Core rejects an ambiguous route match between them). Every action but one calls `ToActionResult()`, the MVC counterpart of `ToMinimalApiResult()`:

```csharp
[HttpGet]
public async Task<IActionResult> List(bool onlyIncomplete, CancellationToken ct)
{
    var result = await sender.Send(new ListTodoTasksQuery(onlyIncomplete), ct);
    return result.ToActionResult();
}
```

`TodoTasksController.GetById` is the deliberate exception - instead of calling `ToActionResult()`, it pattern-matches the outcome's cases directly:

```csharp
[HttpGet("{id:int}")]
public async Task<IActionResult> GetById(int id, CancellationToken ct)
{
    var result = await sender.Send(new GetTodoTaskByIdQuery(id), ct);
    return result.Value switch
    {
        TodoTaskDto dto => this.Ok(dto),
        NotFound notFound => this.NotFound(notFound.Message),
        _ => throw new NotImplementedException(),
    };
}
```

This is a real, compiler-checked exhaustive switch over the union - the same thing `ToActionResult()` does internally, spelled out by hand. `UsersController.GetById` handles the identically-shaped `Outcome<UserDto, NotFound>` union via `ToActionResult()` instead, so the two actions together show both styles applied to the same union shape: manual matching gives one action full control over its response, at the cost of keeping that mapping in sync by hand; `ToActionResult()` centralizes it once for every other action in both controllers.

Neither controller changes anything about the pipeline. Both dispatch through `ISender.Send()` exactly like the minimal-API endpoints do, so the same registered chain (Logging → Auditing → Authorization → Validation → UnitOfWork) runs regardless of which transport a request came in through - see the next section for a `curl` example that shows identical `LoggingBehavior` output for both.

## Running and testing the project

From the repository root:

```bash
dotnet run --project TutorialProject/TodoTasks.Api
```

The app opens an in-memory SQLite database (SQLite's shared-cache mode, kept alive for the app's lifetime) and exports its schema on startup - no separate migration step. Exercise it with `curl`:

```bash
curl -X POST http://localhost:5000/users -H "Content-Type: application/json" -d '{"displayName":"Ada","email":"ada@example.com"}'
curl -X POST http://localhost:5000/todo-tasks -H "Content-Type: application/json" -H "X-User-Id: 1" -d '{"title":"Buy milk"}'
curl http://localhost:5000/todo-tasks/1
curl -X POST http://localhost:5000/todo-tasks/1/complete -H "X-User-Id: 1"
curl -X DELETE http://localhost:5000/users/1
```

The `/api/` controller surface answers the same requests:

```bash
curl http://localhost:5000/api/todo-tasks/1
```

Watch the console for both calls above (`GET /todo-tasks/1` and `GET /api/todo-tasks/1`) - each prints the same `Interlocutor.Abstractions.Pipeline.LoggingBehavior` entry (`Request GetTodoTaskByIdQuery completed with TodoTaskDto in ...ms`), the concrete evidence that dispatching from a controller runs through the identical pipeline as dispatching from a minimal-API endpoint.

`tests/TodoTasks.Api.Tests` (see its own [README](../tests/TodoTasks.Api.Tests/README.md)) covers this project across three layers - unit tests per handler/validator, NHibernate mapping/persistence tests against real in-memory SQLite, and full HTTP integration tests via `WebApplicationFactory<Program>` - run them the normal way:

```bash
dotnet test tests/TodoTasks.Api.Tests/TodoTasks.Api.Tests.csproj
```

## Links

- [Builder & Registration](builder-and-registration.md) - assembly scanning vs. compile-time generated registrations
- [Pipeline & Behaviors](pipeline-and-behaviors.md) - the recommended chain, resource authorization, short-circuiting
- [Notifications & Fan-out](notifications-and-fan-out.md) - contravariant dispatch, execution order, publish strategies
- [Transactions & Store Integration](transactions-and-store-integration.md) - `ITransactionalRequest`, commit/rollback semantics
- [ASP.NET Core Integration & HTTP Mapping](aspnetcore-integration.md) - `ToActionResult()`/`ToMinimalApiResult()`, status code mapping
- [Beginner's Guide: Building a To-Do API](beginners-guide-todo-api.md) - the guide this project implements end to end
