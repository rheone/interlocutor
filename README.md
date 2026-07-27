# Mediance

A mediator/CQRS pipeline library for .NET built on C# 15 union types.

Named after the legal term for how a case is resolved — maps to `Result<T>` outcomes (succeeded, not found, forbidden, etc.).

## Quick Start

```csharp
// 1. Register Mediance
services.AddMediance(typeof(MyHandler).Assembly);

// 2. Define a query
public record GetUser(int UserId) : IQuery<UserResponse>;

// 3. Implement the handler
public class GetUserHandler : IQueryHandler<GetUser, UserResponse>
{
    public Task<Result<UserResponse>> Handle(GetUser query, CancellationToken ct) =>
        Task.FromResult(new Result<UserResponse>(new UserResponse("Alice")));
}

// 4. Send it
var user = await mediator.Send(new GetUser(1));
```

## Packages

| Package | Purpose |
|---------|---------|
| `Mediance.CommonKernel` | Pure data types (`Result<T>`, `Failure`, `PagedResult<T>`). Zero dependencies. |
| `Mediance.Abstractions` | Core mediator contracts, handler interfaces, pipeline behaviors, Mediator implementation. |
| `Mediance.AspNetCore` | `ToActionResult()` HTTP mapping for ASP.NET Core. |
| `Mediance.DependencyInjection.Microsoft` | Assembly-scanning `AddMediance()` for Microsoft.Extensions.DependencyInjection. |
| `Mediance.DependencyInjection.Autofac` | Assembly-scanning `AddMediance()` for Autofac. |
| `Mediance.EntityFrameworkCore` | `TransactionBehavior` for EF Core `DbContext`. |
| `Mediance.NHibernate` | `TransactionBehavior` for NHibernate `ISession`. |
| `Mediance.FluentValidation` | `ValidationBehavior` and `BulkItemValidationBehavior`. |

## Result Types

Mediance uses C# 15 discriminated unions for error handling:

```csharp
readonly union Result<T>(T, Failure);
readonly union Result(Success, Failure);
readonly union Failure(ValidationFailed, RuleViolation, NotFound, Forbidden, Conflict);
```

Match on the result to handle success and all failure cases explicitly.

## Pipeline Behaviors

Cross-cutting concerns are composed as pipeline behaviors:

- **ValidationBehavior** — FluentValidation integration
- **TransactionBehavior** — Unit-of-work (EF Core / NHibernate)
- **LoggingBehavior** — Request/response logging with `ILogger<T>`
- **AuthorizationBehavior** — Declarative policy authorization (`IRequiresPolicy`)
- **AuditingBehavior** — Audit trail via `IAuditSink`
- **PipelineStageBehavior** — Generic ingress/egress hooks

Behaviors compose in an onion/Russian-doll model via `IPipelineBehavior<TRequest, TResponse>`.

## Interface Segregation

- `ISender` — Send-only (request/response)
- `IPublisher` — Publish-only (fire-and-forget notifications)
- `IMediator` — Combined convenience interface

## Links

- [PRD](docs/PRD.md)
- [Architecture Decision Records](docs/adr/)
