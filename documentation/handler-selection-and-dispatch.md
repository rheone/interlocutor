# Handler Selection & Dispatch

How Interlocutor finds the handler for a request. See [ADR-0008](../docs/adr/0008-compile-time-handler-discovery.md) (compile-time discovery) and [ADR-0010](../docs/adr/0010-dispatch-scope-fan-out-open-generics-ambiguity.md) (dispatch rules and ambiguity) for the full architectural decisions.

## Compile-Time Discovery

Handlers are discovered at compile time via a Roslyn source generator, not at runtime via reflection. This means:

- **Missing handlers are build errors**, not first-request failures.
- **Ambiguous handlers are build errors**, not silent resolution.
- **AOT and trim-friendly** — no runtime reflection needed.

Every assembly containing handlers emits a generated module that the application-level generator aggregates. No runtime configuration or scanning is needed.

```csharp
// In your domain assembly:
public record GetUser(int UserId) : IQuery<OperationOutcome<UserDto, NotFound>>;

public class GetUserHandler : IQueryHandler<GetUser, OperationOutcome<UserDto, NotFound>>
{
    public async Task<OperationOutcome<UserDto, NotFound>> Handle(
        GetUser query, CancellationToken ct)
    { /* ... */ }
}

// In your application assembly:
services.AddInterlocutor(builder =>
{
    builder.ScanAssembly(typeof(Program).Assembly);  // Registers GetUserHandler
    // If a query type is missing its handler, you'll get a build error, not a runtime error
});
```

## Handler Selection Rules

Given a request, which handler runs? The rules are:

### Single Handler for Queries & Commands

A query or command dispatch always resolves to **exactly one handler**. If more than one handler could serve the request, it's a build error.

```csharp
public record GetUser(int UserId) : IQuery<OperationOutcome<UserDto, NotFound>>;

public class GetUserHandler : IQueryHandler<GetUser, OperationOutcome<UserDto, NotFound>>
{ /* ... */ }

// This would be a BUILD ERROR (ambiguous, two handlers match):
public class GetUserHandlerV2 : IQueryHandler<GetUser, OperationOutcome<UserDto, NotFound>>
{ /* ... */ }
```

### Closed Handler Beats Open Generic

A closed handler wins over an open generic handler when both could apply:

```csharp
// Open generic — handles any GetById<T> query
public class GetByIdHandler<T> : IQueryHandler<GetById<T>, OperationOutcome<T, NotFound>>
{
    public async Task<OperationOutcome<T, NotFound>> Handle(
        GetById<T> query, CancellationToken ct)
    { /* generic implementation */ }
}

// Closed — specific to GetById<User>
public class GetUserByIdHandler : IQueryHandler<GetById<User>, OperationOutcome<User, NotFound>>
{
    public async Task<OperationOutcome<User, NotFound>> Handle(
        GetById<User> query, CancellationToken ct)
    { /* optimized implementation for User */ }
}

// When you send GetById<User>, GetUserByIdHandler runs (closed beats open)
var result = await mediator.Send(new GetById<User>(42));

// When you send GetById<Product>, GetByIdHandler<Product> runs (no closed alternative)
var result = await mediator.Send(new GetById<Product>(99));
```

### No Consideration of Namespace or Assembly

Handler selection is purely type-shape-driven. Namespace and assembly location never affect which handler runs:

```csharp
// src/MyApp.Domain/GetUserHandler.cs
namespace MyApp.Domain;
public class GetUserHandler : IQueryHandler<GetUser, OperationOutcome<UserDto, NotFound>>
{ /* ... */ }

// src/MyApp.Api/Handlers/GetUserHandler.cs (different namespace, same assembly)
namespace MyApp.Api.Handlers;
public class GetUserHandler : IQueryHandler<GetUser, OperationOutcome<UserDto, NotFound>>
{ /* ... */ }

// BUILD ERROR: Two handlers with the same signature, same reason as above.
// Moving one to a different namespace doesn't change the outcome.
```

### Ambiguity Is a Build Error (With an Override Escape Hatch)

When you need one handler to intentionally supersede another, use the `[OverridesHandler]` attribute:

```csharp
// Open generic
public class LoggingQueryHandler<TQuery, TResponse> 
    : IQueryHandler<TQuery, TResponse>
    where TQuery : IQuery<TResponse>
{ /* logs all queries */ }

// Closed — opt out of logging for a specific query
[OverridesHandler(typeof(LoggingQueryHandler<,>))]
public class GetSecretDataHandler 
    : IQueryHandler<GetSecretData, OperationOutcome<string, Forbidden>>
{
    public async Task<OperationOutcome<string, Forbidden>> Handle(
        GetSecretData query, CancellationToken ct)
    { /* no logging */ }
}

// When you send GetSecretData, GetSecretDataHandler runs (overrides the generic)
var result = await mediator.Send(new GetSecretData());
```

Without the attribute, this would be a build error. The attribute is your explicit declaration that this is intentional.

> [!IMPORTANT]
> The `[OverridesHandler]` attribute lives in `Interlocutor.Abstractions`, so handlers take no additional DI dependency to use it.

## Resource-Based Authorization

Some commands need to authorize based on the loaded entity (ownership, entity state, etc.), not just a policy name. Use `ResourceCommandHandler<TCommand, TEntity, TSuccess>` — a template method implementing the load → authorize → handle pattern:

```csharp
public record DeleteDocument(int DocumentId, int RequestedByUserId)
    : ICommand<OperationOutcome<Success, NotFound, Failure>>;

public class DeleteDocumentHandler 
    : ResourceCommandHandler<DeleteDocument, Document, Success>
{
    private readonly IDocumentRepository repository;

    public DeleteDocumentHandler(IDocumentRepository repository) 
        => this.repository = repository;

    // Step 1: Load the entity
    protected override Task<Document?> LoadAsync(DeleteDocument command, CancellationToken ct)
        => this.repository.FindAsync(command.DocumentId, ct);

    // Step 2: Check authorization against the loaded entity
    protected override Failure? Authorize(Document entity, DeleteDocument command)
    {
        // Return null if authorized, a Failure if denied
        return entity.OwnerId == command.RequestedByUserId
            ? null
            : new Failure("Only the document owner can delete it");
    }

    // Step 3: Handle the command (entity is already loaded and authorized)
    protected override async Task<OperationOutcome<Success, NotFound, Failure>> HandleAsync(
        DeleteDocument command, Document entity, CancellationToken ct)
    {
        await this.repository.DeleteAsync(entity, ct);
        return new Success();
    }
}

// Dispatch is normal:
var result = await mediator.Send(new DeleteDocument(42, currentUserId));
// result is Success, NotFound, or Failure depending on what Authorize/HandleAsync produce
```

**Why is this not a pipeline behavior?** A behavior cannot express "if this specific part fails, produce this specific outcome" for a reason that depends on data loaded mid-pipeline. Resource authorization needs the entity that was just loaded, and passing it forward would require shared mutable state — exactly the architectural pattern Interlocutor avoids. The template method encapsulates the load, authorize, handle sequence as a single cohesive unit, rather than splitting it across multiple layers.

> [!NOTE]
> `ResourceCommandHandler` always returns `OperationOutcome<TSuccess, NotFound, Failure>` — it can produce `NotFound` (when `LoadAsync` returns null) and `Failure` (when `Authorize` returns a failure), and any other outcome depends on what `HandleAsync` produces. The shape is fixed and enforced by the handler's own contract.

## Contrast: Resource vs. Non-Resource Authorization

- **Non-resource authorization** (policy-based, `IRequiresPolicy` marker) — evaluated by `AuthorizationBehavior` before the handler runs. Decides whether the caller can *attempt* a command, without needing to load anything first.
- **Resource authorization** (entity-based, `ResourceCommandHandler`) — decided after loading, based on the loaded entity's state or the caller's relationship to it.

Both are useful and solve different problems:

```csharp
// Non-resource: "Can this user DELETE (any) document?"
public record DeleteDocument(int DocumentId, int RequestedByUserId)
    : ICommand<OperationOutcome<Success, NotFound, Failure>>, 
      IRequiresPolicy  // Evaluated before loading
{
    public string PolicyName => "DeleteDocument";
}

public class AdminOnlyPolicyEvaluator : IPolicyEvaluator
{
    // ... checks if user is admin, short-circuits with Forbidden if not
}

// Resource: "Can this user DELETE *this specific* document?"
public record DeleteDocument(int DocumentId, int RequestedByUserId)
    : ICommand<OperationOutcome<Success, NotFound, Failure>>;  // No marker

public class DeleteDocumentHandler : ResourceCommandHandler<DeleteDocument, Document, Success>
{
    protected override Failure? Authorize(Document entity, DeleteDocument command)
    {
        // Checked after loading — based on entity ownership, status, etc.
        return entity.OwnerId == command.RequestedByUserId ? null : new Failure("Not the owner");
    }
}
```

Use non-resource authorization for coarse gating (role checks, feature flags). Use resource authorization for fine-grained access (ownership, entity state).

## Notifications (Multiple Handlers Allowed)

Notifications are different from queries/commands — they support contravariant fan-out, meaning one notification can match multiple handlers:

```csharp
public record OrderShipped(int OrderId, string TrackingNumber) : INotification;

public class OrderShippedEmailHandler : INotificationHandler<OrderShipped>
{
    public async Task Handle(OrderShipped notification, CancellationToken ct)
    { /* send email */ }
}

public class OrderShippedSmsHandler : INotificationHandler<OrderShipped>
{
    public async Task Handle(OrderShipped notification, CancellationToken ct)
    { /* send SMS */ }
}

// Both handlers run when the notification is published
await mediator.Publish(new OrderShipped(123, "TRACK-456"));
```

More than one handler for a notification is expected and normal. More than one handler for a query/command is a build error. See [notifications-and-fan-out.md](notifications-and-fan-out.md) for the full story on notification dispatch.

## Runtime-Typed Dispatch

In infrastructure code (outbox replay, queue consumers), you may not know the request type until runtime. Use the runtime-typed dispatch overload:

```csharp
// Outbox consumer that deserializes a message into an unknown-until-runtime request type
var requestType = Type.GetType(message.RequestType);  // Runtime type token
var request = JsonSerializer.Deserialize(message.Payload, requestType);

// Runtime-typed dispatch — no generic parameter
var result = await mediator.Send(request);  // request is object

// Result is IOperationOutcome (the base interface all outcomes implement)
// You can inspect success/failure without knowing the concrete outcome shape
```

This is backed by a generated type switch that routes to the strongly-typed dispatch path. See [runtime-typed-dispatch.md](runtime-typed-dispatch.md) for more detail.
