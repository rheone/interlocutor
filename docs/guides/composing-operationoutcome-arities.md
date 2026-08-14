# Composing commands, queries, and handlers around each `OperationOutcome` arity

`Interlocutor.CommonKernel` declares a family of `IOperationOutcome`-implementing unions, distinguished purely
by how many outcome shapes ("arms") they carry:

| Type | Arms | Declared as |
|---|---|---|
| `OperationOutcome` | 2, fixed | `union OperationOutcome(Success, Failure)` |
| `OperationOutcome<TSuccessResult, TFailureResult>` | 2, generic | `union OperationOutcome<TSuccessResult, TFailureResult>(TSuccessResult, TFailureResult)` |
| `OperationOutcome<TSuccessResult, TCase2, TCase3>` | 3, generic | `union OperationOutcome<TSuccessResult, TCase2, TCase3>(TSuccessResult, TCase2, TCase3)` |
| `OperationOutcome<TSuccessResult, TCase2, TCase3, TCase4>` | 4, generic | `union OperationOutcome<TSuccessResult, TCase2, TCase3, TCase4>(TSuccessResult, TCase2, TCase3, TCase4)` |
| `OperationOutcome<TSuccessResult, TCase2, TCase3, TCase4, TCase5>` | 5, generic | `union OperationOutcome<TSuccessResult, TCase2, TCase3, TCase4, TCase5>(TSuccessResult, TCase2, TCase3, TCase4, TCase5)` |

They coexist by arity, the same way `Action`/`Action<T>`/`Action<T1,TCase2>` coexist in the BCL —
`OperationOutcome` is not a special case of `OperationOutcome<T1,TCase2>`, it's a sibling declaration with its own fixed
two cases (`Success`/`Failure`).

Nothing in the mediator, the pipeline, or the DI wiring is hardcoded to one arity.
`ICommandHandler<TCommand,TResponse>`, `IQueryHandler<TQuery,TResponse>`,
`IVoidCommandHandler<TCommand,TResult>`, and `IPipelineBehavior<TRequest,TResponse>` are all
generic over `TResponse`/`TResult` — that type parameter can be `OperationOutcome`, `OperationOutcome<A,B>`,
`OperationOutcome<A,B,C,D,E>`, or a plain non-`OperationOutcome` type. `IResponseOutcomeDescriber` (used by
`LoggingBehavior`, `AuditingBehavior`, and `TransactionBehavior`) never inspects which arity
produced a response either — it unwraps the union via `IUnion.Value` and pattern-matches on the
*case type* (`NotFound`, `Forbidden`, `Conflict`, `ValidationFailure`, `Failure`), so those three
behaviors work identically no matter which arity you choose.

The three examples below build a full vertical slice — a command, a query, their handlers, and
the DI composition root — for `OperationOutcome`, `OperationOutcome<TSuccessResult, TFailureResult>`, and
`OperationOutcome<TSuccessResult, TCase2, TCase3, TCase4, TCase5>` respectively, so the differences show up in real code
rather than in isolated type signatures. (The 3- and 4-arm forms sit between examples 2 and 3 on
the same spectrum — same mechanics, just more or fewer arms.)

Shared plumbing referenced by every example:

```csharp
using Interlocutor.Abstractions;
using Interlocutor.Abstractions.Commands;
using Interlocutor.Abstractions.Core;
using Interlocutor.Abstractions.Markers;
using Interlocutor.Abstractions.Pipeline;
using Interlocutor.Abstractions.Queries;
using Interlocutor.CommonKernel;
using Interlocutor.CommonKernel.ResultTypes;
using Interlocutor.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

public interface ICurrentUser
{
    string UserId { get; }
}

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<TimeOffRequest> TimeOffRequests => Set<TimeOffRequest>();
    public DbSet<Order> Orders => Set<Order>();
}

// TransactionBehavior<TRequest,TResponse,TContext> takes three type parameters against
// IPipelineBehavior<,>'s two, so it can't be registered as an open generic directly. Fixing
// TContext with a thin subclass restores the 2-parameter shape the DI container needs — this
// is registered once, in the composition root, and reused by every example below.
public sealed class AppDbTransactionBehavior<TRequest, TResponse>(Lazy<AppDbContext> db)
    : TransactionBehavior<TRequest, TResponse, AppDbContext>(db);
```

---

## Example 1 — `OperationOutcome` (Success, Failure): "Deactivate User"

Use the fixed `OperationOutcome` when a command or query has **no meaningful success payload and exactly
one failure shape** — the caller only needs to know whether it worked. `IsSuccess` is the only
member: there's no data to project out of either arm.

```csharp
public sealed class User
{
    public Guid Id { get; init; }
    public required string Username { get; init; }
    public bool IsActive { get; set; }
}

// --- Command: no payload, void dispatch path -------------------------------

public sealed record DeactivateUserCommand(Guid UserId) : ICommand, IAuditableRequest, ITransactionalRequest
{
    public string ActionName => "User.Deactivate";
}

public sealed class DeactivateUserCommandHandler(AppDbContext db) : IVoidCommandHandler<DeactivateUserCommand, OperationOutcome>
{
    public async Task<OperationOutcome> Handle(DeactivateUserCommand command, CancellationToken ct)
    {
        // Step 1 — load
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == command.UserId, ct);
        if (user is null)
        {
            return new Failure($"User {command.UserId} was not found.");
        }

        // Step 2 — business rule
        if (!user.IsActive)
        {
            return new Failure("User is already inactive.");
        }

        // Step 3 — the operation. No SaveChangesAsync here — AppDbTransactionBehavior commits
        // once the pipeline returns a non-failure OperationOutcome, because this command is
        // ITransactionalRequest.
        user.IsActive = false;
        return new Success();
    }
}

// --- Query: no payload, plain outcome ---------------------------------------

public sealed record CheckUsernameAvailableQuery(string Username) : IQuery<OperationOutcome>;

public sealed class CheckUsernameAvailableQueryHandler(AppDbContext db) : IQueryHandler<CheckUsernameAvailableQuery, OperationOutcome>
{
    public async Task<OperationOutcome> Handle(CheckUsernameAvailableQuery query, CancellationToken ct)
    {
        var normalized = query.Username.Trim().ToLowerInvariant();
        var taken = await db.Users.AnyAsync(u => u.Username.ToLower() == normalized, ct);

        return taken ? new Failure("Username is already taken.") : new Success();
    }
}
```

**Composition root:**

```csharp
services.AddScoped<IVoidCommandHandler<DeactivateUserCommand, OperationOutcome>, DeactivateUserCommandHandler>();
services.AddScoped<IQueryHandler<CheckUsernameAvailableQuery, OperationOutcome>, CheckUsernameAvailableQueryHandler>();

// Open-generic — applies to every request in the app, regardless of arity:
services.AddScoped(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
services.AddScoped(typeof(IPipelineBehavior<,>), typeof(AuditingBehavior<,>));
services.AddScoped(typeof(IPipelineBehavior<,>), typeof(AppDbTransactionBehavior<,>));
```

**Call site:**

```csharp
var OperationOutcome = await sender.Send<OperationOutcome>(new DeactivateUserCommand(userId), ct);

var response = OperationOutcome.Value switch
{
    Success => Results.NoContent(),
    Failure failure => Results.BadRequest(failure.ToString()),
    _ => throw new UnreachableException(),
};
```

Note the explicit `Send<OperationOutcome>(...)` type argument — `ICommand` (non-generic) carries no
information about which `IOperationOutcome` its handler returns, so the caller must say which one it wants
resolved. Compare this to the query call (`sender.Send(new CheckUsernameAvailableQuery(...), ct)`)
which needs no type argument at all, because `IQuery<TResponse>` declares its response type
directly on the query itself.

---

## Example 2 — `OperationOutcome<TSuccessResult, TFailureResult>`: "Approve Time-Off Request"

Use the 2-arm generic form when there **is** a success payload, but only **one** distinguishable
failure shape. Picking a well-known `ResultTypes` case as `TFailureResult` — here, `Forbidden` —
lets a matching pipeline behavior (`AuthorizationBehavior<TRequest,TSuccess>`) short-circuit the
pipeline generically, before the handler ever runs.

```csharp
public sealed class TimeOffRequest
{
    public Guid Id { get; init; }
    public required string Status { get; set; } // "Pending" | "Approved"
    public required string RequesterUserId { get; init; }
}

public sealed record TimeOffRequestDto(Guid Id, string Status);

// --- Command -----------------------------------------------------------------

public sealed record ApproveTimeOffRequestCommand(Guid RequestId)
    : ICommand<OperationOutcome<TimeOffRequestDto, Forbidden>>, IRequiresPolicy, ITransactionalRequest, IAuditableRequest
{
    public string PolicyName => "timeoff.approve"; // <-- checked by AuthorizationBehavior, below
    public string ActionName => "TimeOff.Approve";
}

public sealed class ApproveTimeOffRequestCommandHandler(AppDbContext db)
    : ICommandHandler<ApproveTimeOffRequestCommand, OperationOutcome<TimeOffRequestDto, Forbidden>>
{
    public async Task<OperationOutcome<TimeOffRequestDto, Forbidden>> Handle(ApproveTimeOffRequestCommand command, CancellationToken ct)
    {
        // Authorization already ran in the pipeline (AuthorizationBehavior short-circuits with
        // Forbidden before this method is ever called if the policy fails) — so the only step
        // left here is the operation itself.
        //
        // Note what this shape *can't* express: there's no NotFound arm. A 2-arm OperationOutcome<TSuccess,
        // TFailure> assumes the entity exists by the time the handler runs. If "the request might
        // not exist" is a real case you need to report distinctly from "forbidden", reach for
        // OperationOutcome<TSuccess, NotFound, Forbidden> (3 arms) instead — see Example 3 for the pattern.
        var request = await db.TimeOffRequests.FirstAsync(r => r.Id == command.RequestId, ct);
        request.Status = "Approved";

        // No SaveChangesAsync — AppDbTransactionBehavior commits on a non-failure OperationOutcome.
        return new TimeOffRequestDto(request.Id, request.Status);
    }
}

// --- Query: same shape, same policy ------------------------------------------

public sealed record GetTimeOffRequestByIdQuery(Guid RequestId)
    : IQuery<OperationOutcome<TimeOffRequestDto, Forbidden>>, IRequiresPolicy
{
    public string PolicyName => "timeoff.view";
}

public sealed class GetTimeOffRequestByIdQueryHandler(AppDbContext db)
    : IQueryHandler<GetTimeOffRequestByIdQuery, OperationOutcome<TimeOffRequestDto, Forbidden>>
{
    public async Task<OperationOutcome<TimeOffRequestDto, Forbidden>> Handle(GetTimeOffRequestByIdQuery query, CancellationToken ct)
    {
        var request = await db.TimeOffRequests.AsNoTracking().FirstAsync(r => r.Id == query.RequestId, ct);
        return new TimeOffRequestDto(request.Id, request.Status);
    }
}
```

**Composition root:**

```csharp
services.AddScoped<ICommandHandler<ApproveTimeOffRequestCommand, OperationOutcome<TimeOffRequestDto, Forbidden>>, ApproveTimeOffRequestCommandHandler>();
services.AddScoped<IQueryHandler<GetTimeOffRequestByIdQuery, OperationOutcome<TimeOffRequestDto, Forbidden>>, GetTimeOffRequestByIdQueryHandler>();

// AuthorizationBehavior<TRequest,TSuccess> implements IPipelineBehavior<TRequest, OperationOutcome<TSuccess,
// Forbidden>> — a *constructed* second type argument, not TSuccess directly — so it can't be
// registered as an open generic (the container has no way to know TSuccess should slot inside
// OperationOutcome<_, Forbidden> rather than replace the whole parameter). Register it per closed
// request/response pair instead, once per command or query that uses this exact shape:
services.AddScoped<
    IPipelineBehavior<ApproveTimeOffRequestCommand, OperationOutcome<TimeOffRequestDto, Forbidden>>,
    AuthorizationBehavior<ApproveTimeOffRequestCommand, TimeOffRequestDto>
>();
services.AddScoped<
    IPipelineBehavior<GetTimeOffRequestByIdQuery, OperationOutcome<TimeOffRequestDto, Forbidden>>,
    AuthorizationBehavior<GetTimeOffRequestByIdQuery, TimeOffRequestDto>
>();

// Logging, Auditing, and the transaction behavior were already registered open-generic in
// Example 1 — nothing more to add here for those three.
```

**Call site:**

```csharp
var OperationOutcome = await sender.Send(new ApproveTimeOffRequestCommand(requestId), ct);

var response = OperationOutcome.Value switch
{
    TimeOffRequestDto dto => Results.Ok(dto),
    Forbidden forbidden => Results.Forbid(),
    _ => throw new UnreachableException(),
};
```

No explicit type argument on `Send` this time — `ICommand<TResponse>` declares its response type
on the command itself, so it's inferred.

---

## Example 3 — `OperationOutcome<TSuccessResult, TCase2, TCase3, TCase4, TCase5>`: "Submit Payment"

Use the widest generic form for operations with several genuinely distinct outcomes that callers
need to handle differently. No built-in pipeline behavior matches an arbitrary 5-arm shape (there
is no `AuthorizationBehavior`-style helper for "success + 4 specific failure arms" — that would
require a distinct registration for every combination), so multi-step logic that would otherwise
live in a pipeline behavior is written explicitly in the handler as a sequence of guard clauses.

```csharp
public sealed class Order
{
    public Guid Id { get; init; }
    public decimal Total { get; init; }
    public bool IsPaid { get; set; }
    public required string OwnerUserId { get; init; }
}

public sealed record PaymentReceiptDto(Guid OrderId, decimal AmountCharged, DateTime ChargedAt);
public sealed record PaymentEligibilityDto(Guid OrderId, decimal RemainingBalance);

// --- Command -------------------------------------------------------------------

public sealed record SubmitPaymentCommand(Guid OrderId, decimal Amount)
    : ICommand<OperationOutcome<PaymentReceiptDto, NotFound, Forbidden, Conflict, ValidationFailure>>,
        ITransactionalRequest,
        IAuditableRequest
{
    public string ActionName => "Payment.Submit";
}

public sealed class SubmitPaymentCommandHandler(AppDbContext db, ICurrentUser currentUser)
    : ICommandHandler<SubmitPaymentCommand, OperationOutcome<PaymentReceiptDto, NotFound, Forbidden, Conflict, ValidationFailure>>
{
    public async Task<OperationOutcome<PaymentReceiptDto, NotFound, Forbidden, Conflict, ValidationFailure>> Handle(
        SubmitPaymentCommand command,
        CancellationToken ct
    )
    {
        // Step 1 — shape validation. (Interlocutor.FluentValidation's ValidationBehavior<,> could
        // take this step instead, the same way AuthorizationBehavior took Example 2's ownership
        // check — but it would need to report through this exact 5-arm shape, so it's inline
        // here for the same reason authorization is inline below.)
        if (command.Amount <= 0)
        {
            return new ValidationFailure("Amount must be greater than zero.");
        }

        // Step 2 — existence
        var order = await db.Orders.FirstOrDefaultAsync(o => o.Id == command.OrderId, ct);
        if (order is null)
        {
            return new NotFound($"Order {command.OrderId} was not found.");
        }

        // Step 3 — ownership. No IRequiresPolicy/AuthorizationBehavior here: that behavior only
        // matches OperationOutcome<TSuccess, Forbidden> (2 arms), not this command's 5-arm response.
        if (order.OwnerUserId != currentUser.UserId)
        {
            return new Forbidden($"User does not own order {order.Id}.");
        }

        // Step 4 — state conflict
        if (order.IsPaid)
        {
            return new Conflict(message: "Order has already been paid.", code: "order.already_paid");
        }

        // Step 5 — the operation. No SaveChangesAsync — AppDbTransactionBehavior commits on a
        // non-failure OperationOutcome.
        order.IsPaid = true;
        return new PaymentReceiptDto(order.Id, command.Amount, DateTime.UtcNow);
    }
}

// --- Query: a "dry run" of the same five outcomes -------------------------------

public sealed record CheckPaymentEligibilityQuery(Guid OrderId, decimal Amount)
    : IQuery<OperationOutcome<PaymentEligibilityDto, NotFound, Forbidden, Conflict, ValidationFailure>>;

public sealed class CheckPaymentEligibilityQueryHandler(AppDbContext db, ICurrentUser currentUser)
    : IQueryHandler<CheckPaymentEligibilityQuery, OperationOutcome<PaymentEligibilityDto, NotFound, Forbidden, Conflict, ValidationFailure>>
{
    public async Task<OperationOutcome<PaymentEligibilityDto, NotFound, Forbidden, Conflict, ValidationFailure>> Handle(
        CheckPaymentEligibilityQuery query,
        CancellationToken ct
    )
    {
        if (query.Amount <= 0)
        {
            return new ValidationFailure("Amount must be greater than zero.");
        }

        var order = await db.Orders.AsNoTracking().FirstOrDefaultAsync(o => o.Id == query.OrderId, ct);
        if (order is null)
        {
            return new NotFound($"Order {query.OrderId} was not found.");
        }

        if (order.OwnerUserId != currentUser.UserId)
        {
            return new Forbidden($"User does not own order {order.Id}.");
        }

        if (order.IsPaid)
        {
            return new Conflict(message: "Order has already been paid.", code: "order.already_paid");
        }

        return new PaymentEligibilityDto(order.Id, order.Total - query.Amount);
    }
}
```

**Composition root:**

```csharp
services.AddScoped<
    ICommandHandler<SubmitPaymentCommand, OperationOutcome<PaymentReceiptDto, NotFound, Forbidden, Conflict, ValidationFailure>>,
    SubmitPaymentCommandHandler
>();
services.AddScoped<
    IQueryHandler<CheckPaymentEligibilityQuery, OperationOutcome<PaymentEligibilityDto, NotFound, Forbidden, Conflict, ValidationFailure>>,
    CheckPaymentEligibilityQueryHandler
>();

// Nothing new needed here — LoggingBehavior, AuditingBehavior, and AppDbTransactionBehavior are
// already registered open-generic from Example 1 and apply to this 5-arm shape exactly as they
// do to OperationOutcome and OperationOutcome<TSuccess,TFailure>, with zero changes.
```

**Call site:**

```csharp
var OperationOutcome = await sender.Send(new SubmitPaymentCommand(orderId, amount), ct);

var response = OperationOutcome.Value switch
{
    PaymentReceiptDto receipt => Results.Ok(receipt),
    NotFound => Results.NotFound(),
    Forbidden => Results.Forbid(),
    Conflict conflict => Results.Conflict(conflict),
    ValidationFailure invalid => Results.BadRequest(invalid.Message),
    _ => throw new UnreachableException(),
};

// OperationOutcome<T1..TCase5>.GetValue() is also available for callers that just need an untyped peek
// at whichever arm is present, without an exhaustive switch — e.g. generic logging:
object? arm = OperationOutcome.GetValue();
```

---

## Summary

| | `OperationOutcome` | `OperationOutcome<TSuccessResult, TFailureResult>` | `OperationOutcome<TSuccessResult, TCase2, TCase3, TCase4, TCase5>` |
|---|---|---|---|
| Success payload | none | `TSuccessResult` | `TSuccessResult` |
| Distinguishable failure shapes | 1 (`Failure`, fixed) | 1 (`TFailureResult`, your choice) | up to 4 (`TCase2`..`TCase5`, your choice) |
| Failure detail available | `Failure.Reason` / `Failure.ErrorCode` | whatever `TFailureResult` carries | whatever each of `TCase2..TCase5` carries |
| Dispatch path | `ICommand` + `IVoidCommandHandler<,>`, or `IQuery<OperationOutcome>` | `ICommand<TResponse>` / `IQuery<TResponse>` with `TResponse = OperationOutcome<...>` | same, just a wider `TResponse` |
| Works with Logging / Auditing / Transaction behaviors | yes, unmodified | yes, unmodified | yes, unmodified |
| Works with `AuthorizationBehavior<TRequest,TSuccess>` | no (no payload to parameterize `TSuccess` with) | yes, when `TFailureResult = Forbidden` | no — shape mismatch; check inline instead |
| Best for | outcome-only operations, no payload | one payload, one well-known failure mode | rich operations with several distinguishable outcomes |
