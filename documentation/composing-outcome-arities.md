# Composing commands, queries, and handlers around each `Outcome` arity

Worked examples for choosing an `Outcome` arity for a given command or query. Assumes [Messages, Result Types & Variance](messages-and-result-types.md)'s vocabulary - read that first if `Outcome`, arities, and marker interfaces are new.

`Interlocutor.CommonKernel` declares a family of `Outcome` types, distinguished purely
by how many outcome shapes ("arms") they carry:

| Type | Arms | Declared as |
|---|---|---|
| `Outcome` | 0, case-less | `record struct Outcome : IUnion` |
| `Outcome<T>` | 1, always-succeeding | `record struct Outcome<T>(T Value) : IUnion` |
| `Outcome<T1, T2>` | 2, generic | `union Outcome<T1, T2>(T1, T2)` |
| `Outcome<T1, T2, T3>` | 3, generic | `union Outcome<T1, T2, T3>(T1, T2, T3)` |
| `Outcome<T1, T2, T3, T4>` | 4, generic | `union Outcome<T1, T2, T3, T4>(T1, T2, T3, T4)` |
| `Outcome<T1, T2, T3, T4, T5>` | 5, generic | `union Outcome<T1, T2, T3, T4, T5>(T1, T2, T3, T4, T5)` |

They coexist by arity, the same way `Action`/`Action<T>`/`Action<T1,T2>` coexist in the BCL -
the case-less `Outcome` is not a special case of `Outcome<T1,T2>`, and `Outcome<T>` is not a
one-case union either, it's a hand-written "fake union" with nothing to discriminate. Both sit
below the generic union family, reserved for a genuinely infallible operation - `Outcome` when
there's no payload to report, `Outcome<T>` when there's exactly one (see Example 1 below for the
boundary between the case-less form and the generic family).

Nothing in the mediator, the pipeline, or the DI wiring is hardcoded to one arity.
`ICommandHandler<TCommand,TResponse>`, `IQueryHandler<TQuery,TResponse>`,
`IBareCommandHandler<TCommand,TResult>`, and `IPipelineBehavior<TRequest,TResponse>` are all
generic over `TResponse`/`TResult` - that type parameter can be `Outcome`, `Outcome<T>`,
`Outcome<A,B>`, `Outcome<A,B,C,D,E>`, or a plain non-`Outcome` type. `IResponseOutcomeDescriber` (used by
`LoggingBehavior`, `AuditingBehavior`, and `UnitOfWorkBehavior`) never inspects which arity
produced a response either - it unwraps the union via `IUnion.Value` and pattern-matches on the
*case type* (`NotFound`, `Forbidden`, `Conflict`, `ValidationError`, `Failure`), so those three
behaviors work identically no matter which arity you choose.

The three examples below build a full vertical slice - a command, a query, their handlers, and
the DI composition root - for `Outcome<T1, T2>`, `Outcome<T1, T2>` again with a different
failure case, and `Outcome<T1, T2, T3, T4, T5>` respectively, so the differences show up in real
code rather than in isolated type signatures. (The 3- and 4-arm forms sit between examples 2 and 3
on the same spectrum - same mechanics, just more or fewer arms. Neither the case-less `Outcome`
nor the single-payload `Outcome<T>` has a worked example of its own here - there's no vertical
slice to build around a type with nothing to project or branch on for `Outcome`, or nothing to
branch on (only ever one case) for `Outcome<T>` - see [Messages, Result Types & Variance § Bare Command](messages-and-result-types.md#bare-command)
for `Outcome`'s one worked example, `ClearCache`, and [Messages, Result Types & Variance § Outcome Unions](messages-and-result-types.md#outcome-unions)
for `Outcome<T>`'s member shape.)

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
using Interlocutor.Persistence.EntityFrameworkCore;
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

// UnitOfWorkBehavior<TRequest,TResponse,TContext> takes three type parameters against
// IPipelineBehavior<,>'s two, so it can't be registered as an open generic directly. Fixing
// TContext with a thin subclass restores the 2-parameter shape the DI container needs - this
// is registered once, in the composition root, and reused by every example below.
public sealed class AppDbUnitOfWorkBehavior<TRequest, TResponse>(Lazy<AppDbContext> db)
    : UnitOfWorkBehavior<TRequest, TResponse, AppDbContext>(db);
```

---

## Example 1 - `Outcome<Success, Failure>`: "Deactivate User"

Use the 2-arm generic form, with `Success`/`Failure` as ordinary case types a consumer names
explicitly, when a command or query has **no meaningful success payload and exactly one failure
shape** - the caller only needs to know whether it worked, via `TryGetValue`/`Match`/`Switch`
pattern matching; there's no data to project out of either arm. This is *not* a job for the
case-less, non-generic `Outcome` - deactivating a user has two real failure modes below (not
found, already inactive), and a response type that can't express them would silently swallow that
distinction. Reach for `Outcome` only once an operation truly has no failure mode to report at all
(`ClearCache` in [Messages, Result Types & Variance](messages-and-result-types.md#bare-command)
is that case).

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

public sealed class DeactivateUserCommandHandler(AppDbContext db)
    : IBareCommandHandler<DeactivateUserCommand, Outcome<Success, Failure>>
{
    public async Task<Outcome<Success, Failure>> Handle(DeactivateUserCommand command, CancellationToken ct)
    {
        // Step 1 - load
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == command.UserId, ct);
        if (user is null)
        {
            return new Failure($"User {command.UserId} was not found.");
        }

        // Step 2 - business rule
        if (!user.IsActive)
        {
            return new Failure("User is already inactive.");
        }

        // Step 3 - the operation. No SaveChangesAsync here - AppDbUnitOfWorkBehavior commits
        // once the pipeline returns a non-failure Outcome, because this command is
        // ITransactionalRequest.
        user.IsActive = false;
        return new Success();
    }
}

// --- Query: no payload, plain outcome ---------------------------------------

public sealed record CheckUsernameAvailableQuery(string Username) : IQuery<Outcome<Success, Failure>>;

public sealed class CheckUsernameAvailableQueryHandler(AppDbContext db)
    : IQueryHandler<CheckUsernameAvailableQuery, Outcome<Success, Failure>>
{
    public async Task<Outcome<Success, Failure>> Handle(CheckUsernameAvailableQuery query, CancellationToken ct)
    {
        var normalized = query.Username.Trim().ToLowerInvariant();
        var taken = await db.Users.AnyAsync(u => u.Username.ToLower() == normalized, ct);

        return taken ? new Failure("Username is already taken.") : new Success();
    }
}
```

**Composition root:**

```csharp
services.AddScoped<IBareCommandHandler<DeactivateUserCommand, Outcome<Success, Failure>>, DeactivateUserCommandHandler>();
services.AddScoped<IQueryHandler<CheckUsernameAvailableQuery, Outcome<Success, Failure>>, CheckUsernameAvailableQueryHandler>();

// Open-generic - applies to every request in the app, regardless of arity:
services.AddScoped(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
services.AddScoped(typeof(IPipelineBehavior<,>), typeof(AuditingBehavior<,>));
services.AddScoped(typeof(IPipelineBehavior<,>), typeof(AppDbUnitOfWorkBehavior<,>));
```

**Call site:**

```csharp
var result = await sender.Send<Outcome<Success, Failure>>(new DeactivateUserCommand(userId), ct);

var response = result.Value switch
{
    Success => Results.NoContent(),
    Failure failure => Results.BadRequest(failure.ToString()),
    _ => throw new UnreachableException(),
};
```

Note the explicit `Send<Outcome<Success, Failure>>(...)` type argument - `ICommand` (non-generic)
carries no information about which `IUnion` its handler returns, so the caller must say which one
it wants resolved. Compare this to the query call (`sender.Send(new CheckUsernameAvailableQuery(...), ct)`)
which needs no type argument at all, because `IQuery<TResponse>` declares its response type
directly on the query itself.

---

## Example 2 - `Outcome<T1, T2>`: "Approve Time-Off Request"

Use the 2-arm generic form when there **is** a success payload, but only **one** distinguishable
failure shape. Picking a well-known `ResultTypes` case as `T2` - here, `Forbidden` -
lets `AuthorizationBehavior<TRequest, TResponse>` short-circuit the pipeline generically, before
the handler ever runs, for a request implementing `IRequiresPolicyRequest`.

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
    : ICommand<Outcome<TimeOffRequestDto, Forbidden>>, IRequiresPolicyRequest, ITransactionalRequest, IAuditableRequest
{
    public string PolicyName => "timeoff.approve"; // <-- checked by AuthorizationBehavior, below
    public string ActionName => "TimeOff.Approve";
}

public sealed class ApproveTimeOffRequestCommandHandler(AppDbContext db)
    : ICommandHandler<ApproveTimeOffRequestCommand, Outcome<TimeOffRequestDto, Forbidden>>
{
    public async Task<Outcome<TimeOffRequestDto, Forbidden>> Handle(ApproveTimeOffRequestCommand command, CancellationToken ct)
    {
        // Authorization already ran in the pipeline (AuthorizationBehavior short-circuits with
        // Forbidden before this method is ever called if the policy fails) - so the only step
        // left here is the operation itself.
        //
        // Note what this shape *can't* express: there's no NotFound arm. A 2-arm Outcome<T1,
        // T2> assumes the entity exists by the time the handler runs. If "the request might
        // not exist" is a real case you need to report distinctly from "forbidden", reach for
        // Outcome<T1, NotFound, Forbidden> (3 arms) instead - see Example 3 for the pattern.
        var request = await db.TimeOffRequests.FirstAsync(r => r.Id == command.RequestId, ct);
        request.Status = "Approved";

        // No SaveChangesAsync - AppDbUnitOfWorkBehavior commits on a non-failure Outcome.
        return new TimeOffRequestDto(request.Id, request.Status);
    }
}

// --- Query: same shape, same policy ------------------------------------------

public sealed record GetTimeOffRequestByIdQuery(Guid RequestId)
    : IQuery<Outcome<TimeOffRequestDto, Forbidden>>, IRequiresPolicyRequest
{
    public string PolicyName => "timeoff.view";
}

public sealed class GetTimeOffRequestByIdQueryHandler(AppDbContext db)
    : IQueryHandler<GetTimeOffRequestByIdQuery, Outcome<TimeOffRequestDto, Forbidden>>
{
    public async Task<Outcome<TimeOffRequestDto, Forbidden>> Handle(GetTimeOffRequestByIdQuery query, CancellationToken ct)
    {
        var request = await db.TimeOffRequests.AsNoTracking().FirstAsync(r => r.Id == query.RequestId, ct);
        return new TimeOffRequestDto(request.Id, request.Status);
    }
}
```

**Composition root:**

```csharp
services.AddScoped<ICommandHandler<ApproveTimeOffRequestCommand, Outcome<TimeOffRequestDto, Forbidden>>, ApproveTimeOffRequestCommandHandler>();
services.AddScoped<IQueryHandler<GetTimeOffRequestByIdQuery, Outcome<TimeOffRequestDto, Forbidden>>, GetTimeOffRequestByIdQueryHandler>();

// AuthorizationBehavior<TRequest, TResponse> is an ordinary open-generic behavior, constrained to
// TRequest : IRequiresPolicyRequest and TResponse : IOutcome - nothing pins TResponse to any
// particular arity. It constructs its Forbidden short-circuit generically via IOutcomeCaseRegistry,
// so one open-generic registration covers every request/response shape that implements IRequiresPolicyRequest
// and has a Forbidden case, regardless of arity:
services.AddScoped(typeof(IPipelineBehavior<,>), typeof(AuthorizationBehavior<,>));

// Both container packages skip constructing this behavior for a request that doesn't implement
// IRequiresPolicyRequest, so this single line is all Example 2 needs - no per-command/query registration.
// Logging, Auditing, and the transaction behavior were already registered open-generic in
// Example 1 - nothing more to add here for those either.
```

**Call site:**

```csharp
var result = await sender.Send(new ApproveTimeOffRequestCommand(requestId), ct);

var response = result.Value switch
{
    TimeOffRequestDto dto => Results.Ok(dto),
    Forbidden forbidden => Results.Forbid(),
    _ => throw new UnreachableException(),
};
```

No explicit type argument on `Send` this time - `ICommand<TResponse>` declares its response type
on the command itself, so it's inferred.

---

## Example 3 - `Outcome<T1, T2, T3, T4, T5>`: "Submit Payment"

Use the widest generic form for operations with several genuinely distinct outcomes that callers
need to handle differently. The ownership check below can't move into `AuthorizationBehavior`
regardless of arity - that behavior only runs *before* the handler, and ownership depends on the
entity the handler loads. This is the same non-resource-vs-resource distinction covered in
[Handler Selection & Dispatch § Contrast: Resource vs. Non-Resource Authorization](handler-selection-and-dispatch.md#contrast-resource-vs-non-resource-authorization) -
`ResourceCommandHandler`'s load-authorize-handle template is the idiomatic place for exactly this
kind of check; it's written as an inline guard clause here to keep this example self-contained and
focused on the arity, not the authorization pattern.

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
    : ICommand<Outcome<PaymentReceiptDto, NotFound, Forbidden, Conflict, ValidationError>>,
        ITransactionalRequest,
        IAuditableRequest
{
    public string ActionName => "Payment.Submit";
}

public sealed class SubmitPaymentCommandHandler(AppDbContext db, ICurrentUser currentUser)
    : ICommandHandler<SubmitPaymentCommand, Outcome<PaymentReceiptDto, NotFound, Forbidden, Conflict, ValidationError>>
{
    public async Task<Outcome<PaymentReceiptDto, NotFound, Forbidden, Conflict, ValidationError>> Handle(
        SubmitPaymentCommand command,
        CancellationToken ct
    )
    {
        // Step 1 - shape validation. (Interlocutor.FluentValidation's ValidationBehavior<,> could
        // take this step instead, the same way AuthorizationBehavior took Example 2's ownership
        // check - but it would need to report through this exact 5-arm shape, so it's inline
        // here for the same reason authorization is inline below.)
        if (command.Amount <= 0)
        {
            return new ValidationError("Amount must be greater than zero.");
        }

        // Step 2 - existence
        var order = await db.Orders.FirstOrDefaultAsync(o => o.Id == command.OrderId, ct);
        if (order is null)
        {
            return new NotFound($"Order {command.OrderId} was not found.");
        }

        // Step 3 - ownership. No IRequiresPolicyRequest/AuthorizationBehavior here: that's non-resource
        // authorization, which runs before the handler and can't see the loaded order. Ownership
        // is a resource-authorization check - depends on the entity above - so it belongs here or
        // in a ResourceCommandHandler, never in a pre-handler behavior, regardless of arity.
        if (order.OwnerUserId != currentUser.UserId)
        {
            return new Forbidden($"User does not own order {order.Id}.");
        }

        // Step 4 - state conflict
        if (order.IsPaid)
        {
            return new Conflict(message: "Order has already been paid.", code: "order.already_paid");
        }

        // Step 5 - the operation. No SaveChangesAsync - AppDbUnitOfWorkBehavior commits on a
        // non-failure Outcome.
        order.IsPaid = true;
        return new PaymentReceiptDto(order.Id, command.Amount, DateTime.UtcNow);
    }
}

// --- Query: a "dry run" of the same five outcomes -------------------------------

public sealed record CheckPaymentEligibilityQuery(Guid OrderId, decimal Amount)
    : IQuery<Outcome<PaymentEligibilityDto, NotFound, Forbidden, Conflict, ValidationError>>;

public sealed class CheckPaymentEligibilityQueryHandler(AppDbContext db, ICurrentUser currentUser)
    : IQueryHandler<CheckPaymentEligibilityQuery, Outcome<PaymentEligibilityDto, NotFound, Forbidden, Conflict, ValidationError>>
{
    public async Task<Outcome<PaymentEligibilityDto, NotFound, Forbidden, Conflict, ValidationError>> Handle(
        CheckPaymentEligibilityQuery query,
        CancellationToken ct
    )
    {
        if (query.Amount <= 0)
        {
            return new ValidationError("Amount must be greater than zero.");
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
    ICommandHandler<SubmitPaymentCommand, Outcome<PaymentReceiptDto, NotFound, Forbidden, Conflict, ValidationError>>,
    SubmitPaymentCommandHandler
>();
services.AddScoped<
    IQueryHandler<CheckPaymentEligibilityQuery, Outcome<PaymentEligibilityDto, NotFound, Forbidden, Conflict, ValidationError>>,
    CheckPaymentEligibilityQueryHandler
>();

// Nothing new needed here - LoggingBehavior, AuditingBehavior, and AppDbUnitOfWorkBehavior are
// already registered open-generic from Example 1 and apply to this 5-arm shape exactly as they
// do to Outcome and Outcome<T1,T2>, with zero changes.
```

**Call site:**

```csharp
var result = await sender.Send(new SubmitPaymentCommand(orderId, amount), ct);

var response = result.Value switch
{
    PaymentReceiptDto receipt => Results.Ok(receipt),
    NotFound => Results.NotFound(),
    Forbidden => Results.Forbid(),
    Conflict conflict => Results.Conflict(conflict),
    ValidationError invalid => Results.BadRequest(invalid.Message),
    _ => throw new UnreachableException(),
};

// Outcome<T1..T5>.GetValue() is also available for callers that just need an untyped peek
// at whichever arm is present, without an exhaustive switch - e.g. generic logging:
object? arm = result.GetValue();
```

---

## Summary

| | `Outcome<Success, Failure>` | `Outcome<T1, T2>` | `Outcome<T1, T2, T3, T4, T5>` |
|---|---|---|---|
| Success payload | none | `T1` | `T1` |
| Distinguishable failure shapes | 1 (`Failure`, your choice of case type) | 1 (`T2`, your choice) | up to 4 (`T2`..`T5`, your choice) |
| Failure detail available | `Failure.Reason` / `Failure.ErrorCode` | whatever `T2` carries | whatever each of `T2..T5` carries |
| Dispatch path | `ICommand` + `IBareCommandHandler<,>`, or `IQuery<Outcome<Success, Failure>>` | `ICommand<TResponse>` / `IQuery<TResponse>` with `TResponse = Outcome<...>` | same, just a wider `TResponse` |
| Works with Logging / Auditing / Transaction behaviors | yes, unmodified | yes, unmodified | yes, unmodified |
| Works with `AuthorizationBehavior<TRequest, TResponse>` for non-resource (`IRequiresPolicyRequest`) checks | no - this arm's `Failure` case can't hold `Forbidden` too, without collapsing two distinct failure reasons into one | yes, when `T2 = Forbidden` | yes, if one of `T2..T5` is `Forbidden` - arity never limits it, only whether a `Forbidden` case exists |
| Works for resource-dependent checks (ownership, entity state) | n/a | n/a | no, at any arity - always needs `ResourceCommandHandler` or an inline check after loading, never a pre-handler behavior |
| Best for | one payload, one well-known failure mode, no distinguishable success payload worth naming | one payload, one well-known failure mode | rich operations with several distinguishable outcomes |

The case-less, non-generic `Outcome` (0 arms, no payload, no failure case at all) and the
single-payload `Outcome<T>` (1 arm, always succeeds) both sit outside this table entirely - neither
is one more point on this spectrum, since there's no failure mode here for either to compare
against. They're reserved for the narrower case where an operation is genuinely infallible: `Outcome`
when it has no payload either (see `ClearCache` in [Messages, Result Types & Variance](messages-and-result-types.md#bare-command)),
`Outcome<T>` when it still hands back exactly one value (see [Messages, Result Types & Variance § Outcome Unions](messages-and-result-types.md#outcome-unions)).
