# Sample App Walkthrough

A guided tour of the runnable project under [`TutorialProject/TodoTasks.Api`](../TutorialProject/TodoTasks.Api/) - a minimal API project that *is* [the Beginner's Guide](beginners-guide-todo-api.md), every "blank" the guide leaves as an exercise filled in: Entity Framework Core against an in-memory SQLite database, FluentValidation, Microsoft.Extensions.DependencyInjection, one command/query/handler/endpoint set per use case. It's a proof-of-concept, excluded from packaging (`IsPackable=false`) and never released - it exists to be read, run, and tested, not shipped.

If you haven't read [Glossary](glossary.md), [Messages, Result Types & Variance](messages-and-result-types.md), and [Builder & Registration](builder-and-registration.md) yet, do that first - this walkthrough assumes the vocabulary and API shape those establish and shows them applied together rather than re-explaining them.

> [!NOTE]
> An earlier version of this project ran on Autofac and NHibernate instead. It wasn't deleted outright: the pre-removal commit is tagged `tutorial-project-nhibernate-autofac`, and `git checkout tutorial-project-nhibernate-autofac -- TutorialProject/TodoTasks.Api tests/TodoTasks.Api.Tests` brings it back into a working tree if you want to see that combination running end to end. The stack shown below is the current one.

## What it is

A to-do list API with two features: **Tasks** (create, look up by id, list, list paged, search, update, complete, reopen, delete, clear completed) and **Users** (create, look up by id, delete). Every piece maps to a piece of Interlocutor's design:

| Project piece | What it demonstrates |
| --- | --- |
| `GetTodoTaskByIdHandler` | A plain query handler - read-only, never transactional |
| `CreateTodoTaskCommand` / `CreateTodoTaskHandler` | A value-returning command handler, with `ITransactionalRequest` protecting a duplicate-title check against a concurrent insert |
| `DeleteTodoTaskCommand` | `IRequiresPolicy`, evaluated before the entity ever loads for the handler's own use |
| `UpdateTodoTaskHandler` | `ResourceCommandHandler`'s load/authorize/handle template - fine-grained, entity-based authorization |
| `DeleteUserHandler` | One `IUnitOfWork` spanning two repositories - a user and every task they own deleted together or not at all |
| `TodoTaskCompletedNotification` with two handlers | A multi-subscriber notification |
| `DevHeaderAuthenticationHandler` | A minimal `AuthenticationHandler` standing in for real authentication, backed by the Users feature's own repository |
| `SearchTodoTasksHandler` | Dropping to `TodoTasksDbContext` directly where `IDomainRepository<TEntity, TKey>`'s fixed method set doesn't reach |
| `SlowHandlerWarningBehavior<,>` | A second custom pipeline behavior beyond the recommended chain, registered innermost |
| `TodoTasksUnitOfWorkBehavior<,>` | The Microsoft DI + EF Core arity workaround the guide's Step 4 describes, made real |
| `ToMinimalApiResult()` at each endpoint | Transport conversion at the edge |

Two things a reader coming from the guide might expect are deliberately absent. There's no MVC-controller transport alongside the minimal-API one, and no `Cancel`-without-failure rollback demo (`IUnitOfWork.Cancel`) - both are illustrative content the guide itself covers inline (see its "Bonus: managing the response manually" and "Bonus: forcing a rollback without a failure outcome" sections) rather than something this project also has to carry. Describing only what this project actually does, rather than carrying those claims forward, is deliberate.

## Project structure

```
TutorialProject/TodoTasks.Api/
├── Domain/                    # TodoTask/User entities and their Dto projections
├── Persistence/                # TodoTasksDbContext - EF Core's fluent mapping lives in its OnModelCreating
├── Infrastructure/             # dev auth handler, policy evaluator, unit-of-work workaround, completion counter,
│   │                           # the slow-handler warning behavior, an IRequestContextAccessor extension
├── Features/
│   ├── Tasks/                  # one folder per use case: CreateTodoTask, GetTodoTaskById, ListTodoTasks,
│   │   │                       # ListTodoTasksPaged, SearchTodoTasks, UpdateTodoTask, CompleteTodoTask,
│   │   │                       # ReopenTodoTask, DeleteTodoTask, ClearCompletedTodoTasks
│   └── Users/                  # CreateUser, GetUserById, DeleteUser
└── Program.cs                  # Microsoft DI host, EF Core + Interlocutor wiring, one MapXEndpoint() call per feature
```

This is vertical-slice structure **by directory within one project**, not one project per slice: every file a use case needs - its command or query, validator (if any), handler, and endpoint mapping - lives together in one folder, e.g. `Features/Tasks/CreateTodoTask/`. `Domain/` and `Persistence/` are the only things shared across slices, because an entity and its mapping genuinely belong to more than one use case at once.

`Features/Tasks/` is deliberately not named `Features/TodoTasks/` - a subfolder segment matching the project's own root namespace (`TodoTasks.Api`) breaks C#'s `using`-resolution for every file under it, since a `using` directive inside a namespace block resolves outward and commits to the first matching segment it finds.

## Query handler example (read request, no transaction)

[`GetTodoTaskByIdQuery`](../TutorialProject/TodoTasks.Api/Features/Tasks/GetTodoTaskById/GetTodoTaskByIdQuery.cs) and its handler are close to the plainest possible shape - a record implementing `IQuery<TResponse>`, a handler implementing `IQueryHandler<TRequest, TResponse>`, no marker interfaces:

```csharp
public sealed record GetTodoTaskByIdQuery(int Id, string OwnerId) : IQuery<Outcome<TodoTaskDto, NotFound>>;

public sealed class GetTodoTaskByIdHandler(IDomainRepository<TodoTask, int> tasks)
    : IQueryHandler<GetTodoTaskByIdQuery, Outcome<TodoTaskDto, NotFound>>
{
    public Task<Outcome<TodoTaskDto, NotFound>> HandleAsync(GetTodoTaskByIdQuery query, CancellationToken ct) =>
        Task.FromResult<Outcome<TodoTaskDto, NotFound>>(
            tasks.GetById(query.Id) is { } task && task.OwnerId == query.OwnerId
                ? TodoTaskDto.FromEntity(task)
                : new NotFound($"TodoTask {query.Id}")
        );
}
```

Two cases, `TodoTaskDto` or `NotFound` - the query never writes, so `UnitOfWorkBehavior` never opens a transaction for it. `OwnerId` (filled in by the endpoint from `IRequestContextAccessor`, never trusted from the request itself) scopes every Tasks read and write in this project to the caller's own tasks; a task that exists but belongs to someone else falls through to the same `NotFound` as a task that doesn't exist at all, deliberately - a distinct `Forbidden` case here would let a caller distinguish "doesn't exist" from "exists, isn't yours" purely from the response shape, leaking the id's existence to a non-owner. Every other Tasks query in this project (`ListTodoTasksQuery`, `ListTodoTasksPagedQuery`, `SearchTodoTasksQuery`) follows the same `OwnerId`-scoping pattern.

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

Three cases: `TodoTaskDto` on success, `ValidationErrors` from [`CreateTodoTaskValidator`](../TutorialProject/TodoTasks.Api/Features/Tasks/CreateTodoTask/CreateTodoTaskValidator.cs) for an empty or overlong title, `Conflict` for a title the caller already has (scoped per-owner: two different callers can each have their own "Buy milk"). `CreateTodoTaskHandler` checks the duplicate itself, as an ordinary read at the top of `Handle` - `ITransactionalRequest` is what keeps that read and the insert that follows it inside one transaction, eagerly begun at pipeline start, so two concurrent requests for the same title can't both pass the check before either commits. `IAuditableRequest`'s `ActionName` records the action via `AuditingBehavior` (see [Pipeline & Behaviors](pipeline-and-behaviors.md)). The handler never commits itself - that's `UnitOfWorkBehavior`'s job, once the handler returns a success outcome.

`TodoTasksDbContext` also declares a unique index on `(OwnerId, Title)` as a schema-level backstop, but the outcome the caller sees comes from the handler's own pre-check, not from catching and translating a constraint-violation exception - the same pattern [`CreateUserHandler`](../TutorialProject/TodoTasks.Api/Features/Users/CreateUser/CreateUserHandler.cs) uses for a duplicate email.

## Authorization: a policy check that's still entity-aware, and a fine-grained check inside the handler

[`DeleteTodoTaskCommand`](../TutorialProject/TodoTasks.Api/Features/Tasks/DeleteTodoTask/DeleteTodoTaskCommand.cs) uses `IRequiresPolicy` - `AuthorizationBehavior` evaluates its `PolicyName` (`"owns-task"`) via [`TodoTasksPolicyEvaluator`](../TutorialProject/TodoTasks.Api/Infrastructure/TodoTasksPolicyEvaluator.cs) *before* the pipeline ever reaches `DeleteTodoTaskHandler`:

```csharp
public sealed class TodoTasksPolicyEvaluator(IDomainRepository<TodoTask, int> tasks, IRequestContextAccessor contextAccessor)
    : IPolicyEvaluator
{
    public const string OwnsTaskPolicyName = "owns-task";

    public Task<bool> EvaluateAsync(string policyName, object request, CancellationToken ct)
    {
        if (policyName != OwnsTaskPolicyName || request is not DeleteTodoTaskCommand command)
        {
            return Task.FromResult(false);
        }

        if (contextAccessor.Context?.UserId is not { } callerId)
        {
            return Task.FromResult(false);
        }

        // No such task: not this evaluator's call to make - let the request through so
        // DeleteTodoTaskHandler produces NotFound instead of this policy masking it as Forbidden.
        var task = tasks.GetById(command.Id);
        var authorized = task is null || task.OwnerId == callerId;
        return Task.FromResult(authorized);
    }
}
```

This evaluator loads the task named by the request and compares its owner against the caller - deliberately, to demonstrate that `AuthorizationBehavior` *can* express an entity-level check when a team wants that decision made before the pipeline ever reaches a handler, even though `IRequiresPolicy` checks are typically coarser ("can this caller attempt this kind of command at all," with no resource in play yet). A task id that doesn't exist is treated as *authorized* here, not denied - whether the task exists at all is left to `DeleteTodoTaskHandler` to answer, so a missing task still produces `NotFound` rather than being masked as `Forbidden` by a policy check that never had an entity to compare ownership against. A non-owner's delete on an existing task is rejected before `DeleteTodoTaskHandler` ever runs; a delete on a nonexistent id is let through by the policy and produces `NotFound` from the handler instead - two different outcomes that are the concrete proof policy enforcement is a separate, earlier pipeline stage from the handler itself.

[`UpdateTodoTaskCommand`](../TutorialProject/TodoTasks.Api/Features/Tasks/UpdateTodoTask/UpdateTodoTaskCommand.cs) makes the identical ownership decision the other, more common way - inside the handler's own template, after the entity is already loaded for the handler's own use:

```csharp
public sealed class UpdateTodoTaskHandler(IDomainRepository<TodoTask, int> tasks, IUnitOfWork unitOfWork)
    : ResourceCommandHandler<UpdateTodoTaskCommand, TodoTask, TodoTaskDto>,
        ICommandHandler<UpdateTodoTaskCommand, Outcome<TodoTaskDto, NotFound, Forbidden>>
{
    protected override Task<TodoTask?> LoadAsync(UpdateTodoTaskCommand command, CancellationToken ct) =>
        Task.FromResult(tasks.GetById(command.Id));

    protected override Forbidden? Authorize(TodoTask entity, UpdateTodoTaskCommand command) =>
        entity.OwnerId == command.RequestedByUserId ? null : new Forbidden("Only the task's owner can update it.");

    protected override async Task<Outcome<TodoTaskDto, NotFound, Forbidden>> HandleAsync(
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

`LoadAsync` returning `null` short-circuits with `NotFound` before `Authorize` ever runs; `Authorize` returning non-null short-circuits with that `Forbidden` before `HandleAsync` ever runs. Neither `NotFound` nor the ownership `Forbidden` reaches `HandleAsync` - it only ever sees an entity that was both found and authorized.

The distinction between the two styles isn't "can the check see the entity" - `TodoTasksPolicyEvaluator` above proves a policy check can. It's *where in the pipeline* the load and the check happen: `AuthorizationBehavior` runs ahead of the handler regardless of what its policy checks, where `ResourceCommandHandler`'s `Authorize` hook can only run after its own `LoadAsync` has already fetched the entity for the handler's own use. [`CompleteTodoTaskHandler`](../TutorialProject/TodoTasks.Api/Features/Tasks/CompleteTodoTask/CompleteTodoTaskHandler.cs) and [`ReopenTodoTaskHandler`](../TutorialProject/TodoTasks.Api/Features/Tasks/ReopenTodoTask/ReopenTodoTaskHandler.cs) make the same ownership check a third way again - hand-written inside an ordinary `ICommandHandler`, not through `ResourceCommandHandler` at all, because their four-case response shape (`Outcome<TodoTaskDto, NotFound, Forbidden, Conflict>`) has a `Conflict` case for the domain-invalid state transition that `ResourceCommandHandler`'s fixed `Outcome<TSuccess, NotFound, Forbidden>` shape has no room for.

## Multi-repository unit-of-work example

[`DeleteUserHandler`](../TutorialProject/TodoTasks.Api/Features/Users/DeleteUser/DeleteUserHandler.cs) is this project's worked example of one `IUnitOfWork` spanning two repositories - deleting a user and every task they own, together or not at all:

```csharp
public sealed class DeleteUserHandler(
    IDomainRepository<User, int> users,
    IDomainRepository<TodoTask, int> tasks,
    IUnitOfWork unitOfWork
) : ICommandHandler<DeleteUserCommand, Outcome<Success, NotFound>>
{
    public async Task<Outcome<Success, NotFound>> HandleAsync(DeleteUserCommand command, CancellationToken ct)
    {
        if (users.GetById(command.Id) is not { } user)
        {
            return new NotFound($"User {command.Id}");
        }

        var ownedTasks = await tasks
            .GetManyAsync(t => t.OwnerId == user.Id.ToString(CultureInfo.InvariantCulture), cancellationToken: ct)
            .ConfigureAwait(false);

        foreach (var task in ownedTasks)
        {
            await tasks.DeleteAsync(task, ct).ConfigureAwait(false);
        }

        await users.DeleteAsync(user, ct).ConfigureAwait(false);

        // One commit for both repositories' staged deletes.
        await unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);

        return default(Success);
    }
}
```

Both repositories are built over the same scoped `Lazy<TodoTasksDbContext>` `IUnitOfWork` commits, so this one `SaveChangesAsync` call is what actually makes the deletion atomic - a task deletion that succeeded while the user deletion failed (or vice versa) is not a state this handler can leave behind. `TodoTask.OwnerId` stays an opaque string rather than a database foreign key to `User.Id` (never trusted from a request body, only ever set from the authenticated caller - see [ASP.NET Core Integration & HTTP Mapping](aspnetcore-integration.md)), so the owned-tasks lookup is a string comparison, not a relational join.

This is load-bearing enough that the project's own persistence tests don't just assert the happy path - `DeleteUserCascadeTests` drives a test-only twin of this handler (`DeleteUserThenFailHandler`/`DeleteUserThenFailCommand`) through the real `UnitOfWorkBehavior<,,>` commit gate, forcing a `Conflict` *after* the deletes have already flushed against the real database, and asserts the DELETE statements that already executed get rolled back - not merely never sent.

## Paging and search

[`ListTodoTasksPagedQuery`](../TutorialProject/TodoTasks.Api/Features/Tasks/ListTodoTasksPaged/ListTodoTasksPagedQuery.cs) is the bounded alternative to the unpaged `ListTodoTasksQuery`. Its handler calls `IDomainRepository<TEntity, TKey>.GetPagedAsync`, which returns `Interlocutor.Persistence.Abstractions.Page<T>` - repository-return-shape vocabulary, not an `Outcome` success-case type (see [Persistence Abstraction](persistence-abstraction.md)). [`PagedTodoTasksDto`](../TutorialProject/TodoTasks.Api/Features/Tasks/ListTodoTasksPaged/PagedTodoTasksDto.cs) maps that `Page<TodoTask>` into a project-owned response type carrying `Items`, `Page`, `PageSize`, and `TotalCount`, the same way `TodoTaskDto` stands in front of the `TodoTask` entity everywhere else in this project.

Both `ListTodoTasksPagedHandler` and the unpaged `ListTodoTasksHandler` sort newest-first, but by different means, for a reason worth knowing before it surprises you: EF Core's SQLite provider can't translate an `ORDER BY` over a `DateTimeOffset` column into SQL (SQLite has no native `DateTimeOffset` type), so an attempt to push `TodoTask.CreatedAtUtc` ordering down to the database throws at query time against the real store, even though the identical code works fine against any mocked `IDomainRepository<TEntity, TKey>`. `ListTodoTasksHandler` sidesteps this by sorting client-side, after materializing an already-fetched list. `ListTodoTasksPagedHandler` can't do that - paging's `Skip`/`Take` has to happen in the database, which means ordering does too - so it orders by `TodoTask.Id` descending instead, which increases in creation order (assigned by the database on insert) and so gives the same newest-first result without hitting the translation gap.

[`SearchTodoTasksQuery`](../TutorialProject/TodoTasks.Api/Features/Tasks/SearchTodoTasks/SearchTodoTasksQuery.cs) rounds out the Tasks read paths - a case-insensitive title search whose handler drops to `TodoTasksDbContext` directly instead of going through `IDomainRepository<TEntity, TKey>`, for the substring-match query that repository interface doesn't expose:

```csharp
public sealed class SearchTodoTasksHandler(Lazy<TodoTasksDbContext> context)
    : IQueryHandler<SearchTodoTasksQuery, Outcome<IReadOnlyList<TodoTaskDto>, Failure>>
{
    public async Task<Outcome<IReadOnlyList<TodoTaskDto>, Failure>> HandleAsync(SearchTodoTasksQuery query, CancellationToken ct)
    {
        var pattern = $"%{query.TitleContains}%";

        var tasks = await context
            .Value.Tasks.Where(t => t.OwnerId == query.OwnerId && EF.Functions.Like(t.Title, pattern))
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return tasks.OrderByDescending(t => t.CreatedAtUtc).Select(TodoTaskDto.FromEntity).ToList();
    }
}
```

`EF.Functions.Like`, not a plain LINQ `.Contains()`: SQLite translates `Contains` to the byte-comparing `instr()` function, which is case-sensitive, while its `LIKE` operator is case-insensitive for ASCII by default - a plain `Contains` would have compiled fine and silently failed the case-insensitivity this search needs. The injected `Lazy<TodoTasksDbContext>` resolves the exact same scoped context every other repository and `IUnitOfWork` in this dispatch shares, so this handler still participates in whatever transaction the unit-of-work behavior is managing - it never opens or holds a `TodoTasksDbContext` of its own.

## Multi-subscriber notification

[`CompleteTodoTaskHandler`](../TutorialProject/TodoTasks.Api/Features/Tasks/CompleteTodoTask/CompleteTodoTaskHandler.cs) publishes [`TodoTaskCompletedNotification`](../TutorialProject/TodoTasks.Api/Features/Tasks/CompleteTodoTask/TodoTaskCompletedNotification.cs) after marking a task complete:

```csharp
task.IsCompleted = true;
await unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);

await publisher.Publish(new TodoTaskCompletedNotification(task.Id, task.OwnerId, task.Title), ct).ConfigureAwait(false);
```

Two independent handlers subscribe to it, neither aware of the other: [`TodoTaskCompletionCounterHandler`](../TutorialProject/TodoTasks.Api/Features/Tasks/CompleteTodoTask/TodoTaskCompletionCounterHandler.cs) increments an in-process per-owner completion count, and [`TodoTaskCompletionAuditLogHandler`](../TutorialProject/TodoTasks.Api/Features/Tasks/CompleteTodoTask/TodoTaskCompletionAuditLogHandler.cs) writes a log entry. See [Notifications & Fan-out](notifications-and-fan-out.md) for execution order guarantees (unspecified) and the Sequential/Parallel publish strategies, and [Transactions & Store Integration](transactions-and-store-integration.md)'s "Transactions with Notifications" section for how a command's transaction and its published notification relate - publishing from inside the handler here, before `UnitOfWorkBehavior` has actually committed, is the guide's own worked example of that trade-off (see its Step 8), not a fix applied only to this project.

## Development authentication

The guide's own "Before you start" section assumes real authentication middleware already exists and deliberately doesn't teach it. This project needs *something* to populate an authenticated caller, so [`DevHeaderAuthenticationHandler`](../TutorialProject/TodoTasks.Api/Infrastructure/DevHeaderAuthenticationHandler.cs) fills that gap - never a pattern to carry into a real application:

```csharp
protected override Task<AuthenticateResult> HandleAuthenticateAsync()
{
    if (
        !this.Request.Headers.TryGetValue(HeaderName, out var headerValues)
        || !int.TryParse(headerValues.ToString(), out var userId)
    )
    {
        return Task.FromResult(AuthenticateResult.NoResult());
    }

    if (users.GetById(userId) is not { } user)
    {
        return Task.FromResult(AuthenticateResult.NoResult());
    }

    var identity = new ClaimsIdentity(
        [new Claim(ClaimTypes.Name, user.Id.ToString(CultureInfo.InvariantCulture))],
        this.Scheme.Name
    );
    var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), this.Scheme.Name);
    return Task.FromResult(AuthenticateResult.Success(ticket));
}
```

It trusts an `X-User-Id` request header as the caller's claimed identity, but only authenticates it once it matches a real `User` row - so create a user first, then act as them by sending that user's id back on every later request. All three outcomes - no header, a header that doesn't parse as an `int`, or a well-formed id matching no `User` - return `AuthenticateResult.NoResult()` rather than `AuthenticateResult.Fail(...)`, so `RequireAuthorization()` rejects each the same ordinary way (a 401), rather than this handler special-casing "invalid" differently from "absent."

It populates `ClaimsIdentity` with the matched user's id as the `ClaimTypes.Name` claim specifically - not `ClaimTypes.NameIdentifier` - because `Interlocutor.AspNetCore.AspNetCoreRequestContextAccessor` reads `RequestContext.UserId` from `HttpContext.User.Identity.Name`, which `ClaimsIdentity` resolves from `ClaimTypes.Name` by default. Ties the Tasks and Users features together: the accessor reads the matched user's id off `HttpContext.User` exactly as the guide describes, nothing custom needed on Interlocutor's side.

`Users` and `POST /todo-tasks/clear-completed`'s sibling read/write endpoints aren't uniformly open, though - every Tasks endpoint calls `.RequireAuthorization()`; the Users endpoints (`Features/Users/CreateUser`, `Features/Users/GetUserById`, `Features/Users/DeleteUser`) don't, since user management itself isn't gated behind caller identity in this proof of concept. A small extension method centralizes the "read the authenticated caller or throw" pattern every authenticated Tasks endpoint needs:

```csharp
public static class RequestContextAccessorExtensions
{
    public static string RequireUserId(this IRequestContextAccessor contextAccessor) =>
        contextAccessor.Context?.UserId
        ?? throw new InvalidOperationException("This endpoint requires an authenticated caller.");
}
```

A missing user id here is a bug, not an outcome the caller can trigger - every endpoint that calls `RequireUserId()` sits behind `RequireAuthorization()`, so this throw is unreachable in practice, matching the guide's own "Before you start" rationale for throwing rather than returning a `Forbidden`/`Failure` case.

## A second custom pipeline behavior

[`SlowHandlerWarningBehavior<TRequest, TResponse>`](../TutorialProject/TodoTasks.Api/Infrastructure/SlowHandlerWarningBehavior.cs) is the guide's own Step 11 worked example, wired in for real: it times the inner `next()` call and logs a warning once a handler alone takes longer than 250ms. It's registered *innermost* - the last `AddBehavior` call in `Program.cs`, after the recommended chain and the unit-of-work workaround below - so its stopwatch starts only after logging, auditing, authorization, validation, and the transaction's own begin have already run, and stops before any of their egress work. Registered anywhere else in the chain, it would measure that surrounding overhead too, not just the handler; `SlowHandlerWarningBehaviorTests` proves this isolation directly, including the case of a slow *ingress* elsewhere in the chain paired with a fast handler, which still doesn't fire this behavior's warning.

## The Microsoft DI + EF Core unit-of-work workaround, made real

The guide's Step 4 describes why `interlocutor.Pipeline.UseRecommendedChain()` can't be used as-is under Microsoft.Extensions.DependencyInjection once `Interlocutor.Persistence.EntityFrameworkCore` is referenced: `UnitOfWorkBehavior<TRequest, TResponse, TContext>`'s extra `TContext` type parameter breaks Microsoft DI's open-generic arity matching. [`TodoTasksUnitOfWorkBehavior<TRequest, TResponse>`](../TutorialProject/TodoTasks.Api/Infrastructure/TodoTasksUnitOfWorkBehavior.cs) is this project's own instance of the workaround the guide prescribes - closing `TContext` over `TodoTasksDbContext`:

```csharp
internal sealed class TodoTasksUnitOfWorkBehavior<TRequest, TResponse>(
    Lazy<TodoTasksDbContext> context,
    IOutcomeClassifier? classifier = null,
    IResponseOutcomeDescriber? describer = null,
    ILogger<UnitOfWorkBehavior<TRequest, TResponse, TodoTasksDbContext>>? logger = null
) : UnitOfWorkBehavior<TRequest, TResponse, TodoTasksDbContext>(context, classifier, describer, logger);
```

`Program.cs` registers the pipeline as `UseRecommendedChain()`'s five individual steps, spelled out, rather than calling that sugar method itself, with this subclass taking `AddUnitOfWorkBehavior()`'s place via the general-purpose `AddBehavior` escape hatch:

```csharp
interlocutor
    .Pipeline.AddLoggingBehavior()
    .AddAuditingBehavior()
    .AddAuthorizationBehavior()
    .AddValidationBehavior()
    .AddBehavior(typeof(TodoTasksUnitOfWorkBehavior<,>))
    .AddBehavior(typeof(SlowHandlerWarningBehavior<,>));
```

See [Transactions & Store Integration § EntityFramework Core Integration](transactions-and-store-integration.md#entityframework-core-integration) for the full mechanism this works around.

## Reordering the pipeline: a dedicated test, not a change to `Program.cs`

The guide's Step 11 also shows composing Validation ahead of Authorization instead of the recommended order, and this project proves the mechanical consequence directly rather than only asserting it in prose. `ReorderedPipelineCompositionTests` composes the real `AuthorizationBehavior<,>`/`ValidationBehavior<,>` behaviors directly (no DI host) against a request that fails both checks: under the recommended order (Authorization outermost), the request is denied without the validator ever running; under the reordered chain (Validation outermost), the request is rejected without the policy evaluator ever being invoked. `Program.cs`'s own live pipeline is untouched - reordering it for real would change every other feature's behavior too, not just prove a point about one request.

## Transport conversion at the edge (ASP.NET Core integration)

Every endpoint calls `ToMinimalApiResult()` as its last step, converting the outcome's active case to an `IResult`:

```csharp
app.MapGet(
    "/todo-tasks/{id:int}",
    async (int id, ISender sender, IRequestContextAccessor contextAccessor, CancellationToken ct) =>
    {
        var ownerId = contextAccessor.RequireUserId();
        var result = await sender.Send(new GetTodoTaskByIdQuery(id, ownerId), ct).ConfigureAwait(false);
        return result.ToMinimalApiResult();
    }
).RequireAuthorization();
```

Handlers never see `IResult`, `HttpContext`, or any other ASP.NET Core type - the mapping happens once, at the endpoint delegate. See [ASP.NET Core Integration & HTTP Mapping](aspnetcore-integration.md) for the case-to-status-code mapping this extension applies.

## Running and testing the project

From the repository root:

```bash
dotnet run --project TutorialProject/TodoTasks.Api
```

The app opens a single, kept-open ADO.NET connection to an in-memory SQLite database (`Data Source=:memory:`), registered as a singleton so it stays open for the app's entire lifetime - SQLite discards an in-memory database the instant its last connection closes, so handing that exact same connection object to every scoped `TodoTasksDbContext` (rather than a fresh connection per scope) is what lets one request's writes stay visible to the next request's reads. There are no migrations for this proof-of-concept - the schema is (re)created from the current `DbContext` model on startup, via `Database.EnsureCreatedAsync()` against that same kept-open connection. `InMemorySqliteConnectionPersistenceTests` (in `tests/TodoTasks.Api.Tests`) proves this strategy actually holds across DI scope boundaries, not just within one.

Exercise the running app with `curl` (the default port is `5140` - see `Properties/launchSettings.json`):

```bash
curl -X POST http://localhost:5140/users -H "Content-Type: application/json" -d '{"displayName":"Ada","email":"ada@example.com"}'
curl -X POST http://localhost:5140/todo-tasks -H "Content-Type: application/json" -H "X-User-Id: 1" -d '{"title":"Buy milk"}'
curl http://localhost:5140/todo-tasks/1 -H "X-User-Id: 1"
curl -X POST http://localhost:5140/todo-tasks/1/complete -H "X-User-Id: 1"
curl -X DELETE http://localhost:5140/users/1
```

A liveness endpoint (`GET /health`, anonymous) and a diagnostic twin (`GET /health/whoami`, authenticated) exist independent of any Tasks/Users feature, so the host itself - DI container, EF Core schema creation, pipeline registration - can be proven up before any feature endpoint exists to exercise instead; `TodoTasksApiFactory` (the `WebApplicationFactory<Program>`-based test fixture) and `DevAuthenticationTests` both lean on `/health/whoami` for exactly this.

`tests/TodoTasks.Api.Tests` covers this project across three layers - unit tests per handler/validator/behavior (`Unit/`), EF Core persistence tests against the real in-memory SQLite database (`Persistence/`), and full HTTP integration tests via `WebApplicationFactory<Program>` (`Http/`) - run them the normal way:

```bash
dotnet test tests/TodoTasks.Api.Tests/TodoTasks.Api.Tests.csproj
```

## Links

- [Builder & Registration](builder-and-registration.md) - assembly scanning vs. compile-time generated registrations
- [Pipeline & Behaviors](pipeline-and-behaviors.md) - the recommended chain, resource authorization, short-circuiting
- [Notifications & Fan-out](notifications-and-fan-out.md) - contravariant dispatch, execution order, publish strategies
- [Transactions & Store Integration](transactions-and-store-integration.md) - `ITransactionalRequest`, commit/rollback semantics, the Microsoft DI + EF Core arity workaround
- [ASP.NET Core Integration & HTTP Mapping](aspnetcore-integration.md) - `ToMinimalApiResult()`, status code mapping
- [Beginner's Guide: Building a To-Do API](beginners-guide-todo-api.md) - the guide this project implements end to end
