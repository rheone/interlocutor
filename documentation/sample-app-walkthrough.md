# Sample App Walkthrough

A guided tour of the runnable sample under [`samples/`](../samples/) - two projects, `Interlocutor.Sample.Handlers` (a class library) and `Interlocutor.Sample.WebApi` (a minimal API), together showing the intended shape of a real Interlocutor consumer end to end. Both are excluded from packaging (`IsPackable=false`) - they exist to be read and run, not shipped.

If you haven't read [Glossary](glossary.md), [Messages, Result Types & Variance](messages-and-result-types.md), and [Builder & Registration](builder-and-registration.md) yet, do that first - this walkthrough assumes the vocabulary and API shape those establish and shows them applied together rather than re-explaining them.

## What it is

A minimal to-do list API: create, look up by id, delete, and a running count of how many items have been created. Small enough to read in one sitting, deliberately - the point isn't the domain, it's the composition around it. Every feature the sample exercises maps to a piece of Interlocutor's design:

| Sample piece | What it demonstrates |
| --- | --- |
| `GetTodoByIdQuery` / `GetTodoByIdHandler` | A plain query handler - read-only, never transactional |
| `CreateTodoCommand` / `CreateTodoHandler` | A value-returning command handler |
| `DeleteTodoCommand` / `DeleteTodoHandler` | A unit command handler (mutates, returns no domain data) |
| `GetByIdQuery<TEntity>` / `GetByIdHandler<TEntity>` | A generic handler serving more than one concrete request type |
| `Interlocutor.Sample.Handlers` referenced by `Interlocutor.Sample.WebApi` | Cross-assembly handler discovery with no assembly list |
| `Program.cs`'s `interlocutor.Pipeline...` chain | An explicitly composed pipeline, not `UseRecommendedChain()` |
| `DuplicateTitleGuardStage` | A pipeline stage that short-circuits |
| `TodoCreatedNotification` with two handlers | A multi-subscriber notification |
| `ToMinimalApiResult()` at each endpoint | Transport conversion at the edge |

## Project structure

```
samples/
├── Interlocutor.Sample.Handlers/     # domain + application layer: entities, requests, handlers
│   ├── SampleDbContext.cs
│   ├── Shared/                       # the generic GetByIdQuery<TEntity> and its handler
│   ├── Tags/                         # TagItem - exists only to give the generic handler a second entity type
│   └── Todos/                        # TodoItem, its commands/queries/handlers, the guard stage, the notification
└── Interlocutor.Sample.WebApi/       # API layer: minimal API endpoints, DI wiring, EF Core/Sqlite setup
    ├── Program.cs
    ├── SampleUnitOfWorkBehavior.cs
    ├── SampleJsonContext.cs
    └── CompiledModels/               # EF Core compiled model, for the AOT/trim publish story
```

`Interlocutor.Sample.Handlers` never references `Interlocutor.AspNetCore` or ASP.NET Core itself - handlers know nothing about HTTP. `Interlocutor.Sample.WebApi` is the only place transport-specific code lives, which is what "transport conversion at the edge" means in practice: the boundary is a project boundary, not just a convention.

## Query handler example (read request, no transaction)

[`GetTodoByIdQuery`](../samples/Interlocutor.Sample.Handlers/Todos/GetTodoByIdQuery.cs) and its handler are the plainest possible shape - a record implementing `IQuery<TResponse>`, a handler implementing `IQueryHandler<TRequest, TResponse>`, no marker interfaces:

```csharp
public sealed record GetTodoByIdQuery(int Id) : IQuery<OperationOutcome<TodoItem, NotFound>>;

public sealed class GetTodoByIdHandler(SampleDbContext db)
    : IQueryHandler<GetTodoByIdQuery, OperationOutcome<TodoItem, NotFound>>
{
    public async Task<OperationOutcome<TodoItem, NotFound>> Handle(GetTodoByIdQuery query, CancellationToken ct)
    {
        var todo = await db.TodoItems.FindAsync([query.Id], ct);
        return todo is null ? new NotFound($"To-do {query.Id} not found") : todo;
    }
}
```

Two cases, `TodoItem` or `NotFound` - the query never writes, so `UnitOfWorkBehavior` never opens a transaction for it. A read has nothing to commit or roll back, and no marker is needed to say so.

## Command handler example (write request, transaction)

[`CreateTodoCommand`](../samples/Interlocutor.Sample.Handlers/Todos/CreateTodoCommand.cs) is a value-returning command - it mutates state and hands back the entity it created, not just a bare outcome - and it implements `ITransactionalRequest`:

```csharp
public sealed record CreateTodoCommand(string Title)
    : ICommand<OperationOutcome<TodoItem, ValidationErrors, Conflict>>,
        ITransactionalRequest;
```

Three cases: `TodoItem` on success, `ValidationErrors` for an empty title, `Conflict` for a duplicate one. [`CreateTodoHandler`](../samples/Interlocutor.Sample.Handlers/Todos/CreateTodoHandler.cs) only has to handle the first two - the duplicate-title case never reaches it, because [`DuplicateTitleGuardStage`](#pipeline-stage-with-short-circuit) rejects it earlier. The handler doesn't call `SaveChangesAsync` itself; committing is `UnitOfWorkBehavior`'s job, after the handler returns a success outcome - the insert alone would already get a transaction with no marker at all. `ITransactionalRequest` here does a narrower job: it makes the stage's duplicate-title read (below) share that same transaction, eagerly begun at pipeline start, instead of one that would otherwise only open on the insert. See [Transactions & Store Integration](transactions-and-store-integration.md) for how that commit/rollback decision is made from the outcome's active case.

[`DeleteTodoCommand`](../samples/Interlocutor.Sample.Handlers/Todos/DeleteTodoCommand.cs) is the sample's unit command - it implements bare `ICommand` (no `<TResponse>`), returning `OperationOutcome<Success, NotFound>` with no domain payload. It has no preceding read to protect, so it doesn't need `ITransactionalRequest` at all - its delete gets a transaction automatically, the same begin-on-first-write way `CreateTodoCommand`'s insert does.

## Generic handler example (serves multiple request types)

[`GetByIdQuery<TEntity>`](../samples/Interlocutor.Sample.Handlers/Shared/GetByIdQuery.cs) and [`GetByIdHandler<TEntity>`](../samples/Interlocutor.Sample.Handlers/Shared/GetByIdHandler.cs) are generic over the entity type, so one handler serves every closed instantiation instead of one handler per entity:

```csharp
public sealed record GetByIdQuery<TEntity>(int Id) : IQuery<OperationOutcome<TEntity, NotFound>>
    where TEntity : class;

public sealed class GetByIdHandler<TEntity>(SampleDbContext db)
    : IQueryHandler<GetByIdQuery<TEntity>, OperationOutcome<TEntity, NotFound>>
    where TEntity : class
{
    public async Task<OperationOutcome<TEntity, NotFound>> Handle(GetByIdQuery<TEntity> query, CancellationToken ct)
    {
        var entity = await db.Set<TEntity>().FindAsync([query.Id], ct);
        return entity is null ? new NotFound($"{typeof(TEntity).Name} {query.Id} not found") : entity;
    }
}
```

`TagItem` exists in the sample for exactly one reason: to give this handler a second entity type to actually serve, so the sample proves "more than one request type" rather than asserting it.

> [!IMPORTANT]
> Interlocutor.Generator only closes an open generic handler over a request type when it finds a *fully closed usage* of that instantiation (for example `new GetByIdQuery<TodoItem>(...)`) in the **same compilation** as the handler - it doesn't scan a referencing project's syntax trees for usages. [`GetByIdQueries`](../samples/Interlocutor.Sample.Handlers/Shared/GetByIdQueries.cs), beside the handler in `Interlocutor.Sample.Handlers`, supplies those closed usages for both entity types via typed extension methods (`GetTodoByIdGenericAsync`, `GetTagByIdAsync`). `Interlocutor.Sample.WebApi` dispatches through those instead of constructing `GetByIdQuery<TEntity>` directly - constructing it at the call site there would be invisible to the generator's closing pass.

## Cross-assembly handler discovery

`Interlocutor.Sample.WebApi` never lists an assembly to scan. Its `Program.cs` calls:

```csharp
interlocutor.AddGeneratedRegistrations(
    Interlocutor.Generated.Interlocutor_Sample_WebApi.InterlocutorGeneratedRegistrations.Registrations
);
```

`Interlocutor.Sample.WebApi`'s own generated registration module aggregates `Interlocutor.Sample.Handlers`' generated module automatically, because the web project references the handler library (see [Builder & Registration](builder-and-registration.md) for how compile-time aggregation across a project reference works, and [Dependency Injection: Abstractions & Registration Model](dependency-injection-abstractions.md#compile-time-generated-registrations) for the generated-registration attributes underneath it). One `AddGeneratedRegistrations` call, no assembly list anywhere in either project, and every handler in both assemblies is registered.

## Explicitly composed pipeline (non-default behaviors)

`Program.cs` composes the pipeline by hand rather than calling `UseRecommendedChain()`:

```csharp
interlocutor
    .Pipeline.AddLoggingBehavior()
    .AddBehavior(typeof(SampleUnitOfWorkBehavior<,>))
    .AddStage<DuplicateTitleGuardStage>()
    .AddStageHost();
```

That's deliberate: the sample's point is that a consumer chooses exactly the behaviors their application needs and the order they run in (see [Pipeline & Behavior Composition](pipeline-and-behaviors.md) for the full behavior list and the reasoning behind the recommended order). Logging wraps everything; the unit-of-work behavior wraps the stage host - not the other way around, unlike the library's own recommended chain, which places it innermost - specifically so the guard stage's duplicate-title read and the handler's insert that follows it share one transaction, per `CreateTodoCommand`'s `ITransactionalRequest` marker. `AddStageHost()` is what actually runs the registered stages, since a stage isn't itself an `IPipelineBehavior`.

The unit-of-work behavior is registered as `SampleUnitOfWorkBehavior<,>`, not the library's own `UnitOfWorkBehavior<,,>`, for a container-mechanics reason worth knowing if you write your own EF Core-backed application: Microsoft.Extensions.DependencyInjection requires an open generic registration's implementation type to leave the *same* number of type parameters open as the service interface it's registered against, so a three-parameter `UnitOfWorkBehavior<TRequest, TResponse, TContext>` cannot be registered directly behind two-parameter `IPipelineBehavior<TRequest, TResponse>`. [`SampleUnitOfWorkBehavior<TRequest, TResponse>`](../samples/Interlocutor.Sample.WebApi/SampleUnitOfWorkBehavior.cs) fixes `TContext` to `SampleDbContext` at compile time and is what actually gets registered.

## Pipeline stage with short-circuit

[`DuplicateTitleGuardStage`](../samples/Interlocutor.Sample.Handlers/Todos/DuplicateTitleGuardStage.cs) declines a `CreateTodoCommand` whose title already exists, before the handler ever runs - inside the same transaction as the insert that would otherwise follow it, per the pipeline ordering above:

```csharp
public sealed class DuplicateTitleGuardStage(SampleDbContext db)
    : PipelineStageBase<CreateTodoCommand, OperationOutcome<TodoItem, ValidationErrors, Conflict>>
{
    public override async Task<StageResult<OperationOutcome<TodoItem, ValidationErrors, Conflict>>> OnBeforeAsync(
        CreateTodoCommand request,
        CancellationToken ct
    )
    {
        var title = request.Title.Trim();
        var exists = await db.TodoItems.AnyAsync(t => t.Title == title, ct);
        return exists
            ? StageResult<OperationOutcome<TodoItem, ValidationErrors, Conflict>>.ShortCircuit(
                new Conflict($"A to-do titled '{title}' already exists.", "duplicate-title")
            )
            : StageResult<OperationOutcome<TodoItem, ValidationErrors, Conflict>>.Continue;
    }
}
```

`CreateTodoHandler` never has to consider the duplicate-title case at all - it's handled entirely by the stage, one layer up. See [Pipeline & Behavior Composition](pipeline-and-behaviors.md) for `IPipelineStage`'s full before/after contract.

## Multi-subscriber notification

After `CreateTodoCommand`'s transaction commits, `Program.cs` publishes [`TodoCreatedNotification`](../samples/Interlocutor.Sample.Handlers/Todos/TodoCreatedNotification.cs):

```csharp
var result = await sender.Send(new CreateTodoCommand(request.Title), ct);
if (result.Value is TodoItem created)
{
    await publisher.Publish(new TodoCreatedNotification(created.Id, created.Title), ct);
}
```

Two independent handlers subscribe to it, neither aware of the other: [`TodoCreatedLoggingHandler`](../samples/Interlocutor.Sample.Handlers/Todos/TodoCreatedLoggingHandler.cs) logs the creation, and [`TodoCreatedCounterHandler`](../samples/Interlocutor.Sample.Handlers/Todos/TodoCreatedCounterHandler.cs) increments an in-process counter exposed at `GET /todos/created-count`. Publishing happens after the command's own dispatch returns, not from inside the transaction - by the time it fires, the create has already committed, so the notification announces a fact that's true in the database rather than one that might still roll back. See [Notifications & Fan-out](notifications-and-fan-out.md) for execution order guarantees (unspecified) and the Sequential/Parallel publish strategies.

## Transport conversion at the edge (ASP.NET Core integration)

Every endpoint in `Program.cs` calls `ToMinimalApiResult()` as its last step, converting the outcome's active case to an `IResult`:

```csharp
app.MapGet(
    "/todos/{id:int}",
    async (int id, ISender sender, CancellationToken ct) =>
        (await sender.Send(new GetTodoByIdQuery(id), ct)).ToMinimalApiResult()
);
```

Handlers and stages never see `IResult`, `HttpContext`, or any other ASP.NET Core type - the mapping happens once, at the endpoint delegate, which is the entire "transport conversion at the edge" idea in one line. See [ASP.NET Core Integration & HTTP Mapping](aspnetcore-integration.md) for the case-to-status-code mapping this extension applies.

A couple of other pieces exist purely to make that mapping work under a trimmed publish, since this sample also proves the library's trim story (see [Trim & AOT Support](trim-and-aot-support.md)):

- [`SampleJsonContext`](../samples/Interlocutor.Sample.WebApi/SampleJsonContext.cs) - a source-generated `JsonSerializerContext` listing every request/response body type an endpoint reads or writes, including the failure-case bodies (`ProblemDetails`, `Success`) that are easy to overlook because nothing references them by name.
- `CompiledModels/` - an EF Core compiled model (regenerated via `dotnet ef dbcontext optimize` whenever `SampleDbContext`'s shape changes), replacing EF Core's normal reflection-based model building.

## Running and testing the sample

From the repository root:

```bash
dotnet run --project samples/Interlocutor.Sample.WebApi --urls http://localhost:5299
```

The app opens an in-memory Sqlite database and creates its schema on startup - no separate migration step. Exercise it with `curl`:

```bash
curl -X POST http://localhost:5299/todos -H "Content-Type: application/json" -d '{"title":"buy milk"}'
curl http://localhost:5299/todos/1
curl http://localhost:5299/todos/1/generic   # same lookup, through the generic handler
curl http://localhost:5299/todos/created-count
curl -X DELETE http://localhost:5299/todos/1
```

[`scripts/smoke-test-trimmed-sample.sh`](../scripts/smoke-test-trimmed-sample.sh) is the automated version of the same walk - create, read via both handlers, a 404 for a tag that doesn't exist, the fan-out counter, a duplicate-title conflict, delete, and a final 404 - run against a published (trimmed) binary in CI so a build that compiles clean can't silently mean a build that crashes on first dispatch. Run it yourself against a local build:

```bash
dotnet publish samples/Interlocutor.Sample.WebApi -c Release -o /tmp/sample-publish
scripts/smoke-test-trimmed-sample.sh /tmp/sample-publish/Interlocutor.Sample.WebApi
```

## Links

- [Builder & Registration](builder-and-registration.md) - assembly scanning vs. compile-time generated registrations, cross-assembly aggregation
- [Pipeline & Behaviors](pipeline-and-behaviors.md) - the recommended chain, pipeline stages, short-circuiting
- [Notifications & Fan-out](notifications-and-fan-out.md) - contravariant dispatch, execution order, publish strategies
- [Transactions & Store Integration](transactions-and-store-integration.md) - `ITransactionalRequest`, commit/rollback semantics
- [ASP.NET Core Integration & HTTP Mapping](aspnetcore-integration.md) - `ToActionResult()`/`ToMinimalApiResult()`, status code mapping
- [Trim & AOT Support](trim-and-aot-support.md) - what this sample's trimmed-publish smoke test proves
