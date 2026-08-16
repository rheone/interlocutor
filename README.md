# Interlocutor

[![.NET](https://img.shields.io/badge/.NET-11%20%28preview%29-blueviolet)](https://dotnet.microsoft.com/)
[![C#](https://img.shields.io/badge/C%23-15%20%28preview%29-green)](https://learn.microsoft.com/en-us/dotnet/csharp/)
[![License: Apache 2.0](https://img.shields.io/badge/License-Apache%202.0-blue.svg)](LICENSE)

A mediator/CQRS pipeline library for .NET built on C# 15 union types.

Named after the participant who conducts a dialogue — the library mediates requests between your code and its handlers, letting each side speak through typed, compiler-checked outcomes (succeeded, not found, forbidden, etc.) instead of throwing for expected failures. Exceptions stay reserved for bugs and infrastructure faults.

> **Status**: This library has not shipped yet. You're looking at a pre-release implementation. API is subject to change.

## Concepts

### The Mediator Pattern

A structural pattern that defines an object (the mediator) that encapsulates how a set of objects interact. Instead of objects communicating directly, they communicate through the mediator, which coordinates their interaction. Interlocutor implements this as a `IMediator` service that dispatches requests to handlers.

### CQRS (Command Query Responsibility Segregation)

A pattern that separates read operations (queries) from write operations (commands) into distinct request types. Queries never mutate state; commands may. This separation allows each to be optimized independently — queries can pull from a read-optimized view, commands can ensure transactional consistency. Interlocutor supports both via `IQuery<TResponse>` and `ICommand<TResponse>` / `ICommand`.

### Vertical Slice Architecture

An architectural style where features are organized as thin slices cutting through all layers (presentation, business logic, persistence) rather than organizing by technical layer (all controllers together, all services together, etc.). Each slice is self-contained and can be developed/tested independently. A Vertical Slice Architecture works naturally with CQRS: one query or command per feature slice.

## Quick Start

### 1. Register the library

```csharp
// Program.cs — register Interlocutor and scan for handlers in your assemblies
services.AddInterlocutor(builder =>
{
    // Auto-scan the entry assembly for IQueryHandler, ICommandHandler implementations
    builder.ScanAssembly(typeof(Program).Assembly);
    
    // The pipeline is empty by default — compose exactly the behaviors you need.
    // This example uses the library's documented recommended chain:
    // Logging → Auditing → Authorization → Validation → Transaction
    // (See documentation/pipeline-and-behaviors.md for the full rationale.)
    builder.UseRecommendedChain();
});
```

### 2. Define a request and handler

```csharp
// A query — read data, never writes, returns a result
public record GetUser(int UserId) : IQuery<OperationOutcome<UserDto, NotFound>>;

// The handler — injected normally, no special requirements
public class GetUserHandler : IQueryHandler<GetUser, OperationOutcome<UserDto, NotFound>>
{
    private readonly IUserRepository repository;

    public GetUserHandler(IUserRepository repository) => this.repository = repository;

    public async Task<OperationOutcome<UserDto, NotFound>> Handle(
        GetUser query, CancellationToken ct)
    {
        var user = await repository.FindAsync(query.UserId, ct);
        return user == null
            ? new NotFound($"User {query.UserId} not found")
            : new UserDto(user.Id, user.Name);
    }
}
```

### 3. Send the request

```csharp
// Controllers, services, anywhere you've injected ISender or IMediator
var result = await mediator.Send(new GetUser(42), cancellationToken);

// Pattern-match the result — the compiler forces you to handle every case
var message = result switch
{
    OperationOutcome.SuccessCase<UserDto> success => $"Found: {success.Value.Name}",
    NotFound notfound => $"Not found: {notfound.Message}",
    _ => "Unexpected"
};
```

## Library Overview

| Package | Purpose |
|---------|---------|
| `Interlocutor.CommonKernel` | Result types: `OperationOutcome<...>` unions and failure cases (`Success`, `NotFound`, `Forbidden`, `Conflict`, `ValidationError`, `Failure`). Zero dependencies. |
| `Interlocutor.Abstractions` | Core: `IMediator`, `ISender`, `IPublisher`; handler interfaces; pipeline behaviors (`LoggingBehavior`, `AuthorizationBehavior`, `AuditingBehavior`); marker interfaces for opt-in concerns. |
| `Interlocutor.DependencyInjection.Microsoft` | Builder-based registration for Microsoft.Extensions.DependencyInjection. |
| `Interlocutor.DependencyInjection.Autofac` | Builder-based registration for Autofac. |
| `Interlocutor.AspNetCore` | `ToActionResult()` — maps result types to HTTP responses. ASP.NET Core only. |
| `Interlocutor.EntityFrameworkCore` | `TransactionBehavior` for EF Core `DbContext`. |
| `Interlocutor.NHibernate` | `TransactionBehavior` for NHibernate `ISession`. |
| `Interlocutor.FluentValidation` | `ValidationBehavior` — automatic request validation via FluentValidation. |

## Deeper dive

### User documentation

For people using Interlocutor as a library in their own application:

- **[Full documentation index](documentation/index.md)** — detailed guides on handlers, pipelines, result types, notifications, testing, and more.

### Developer documentation

For contributors and maintainers working on *this repository* — not needed to use the library:

- **[Developer documentation index](docs/README.md)** — Architecture Decision Records, the project PRD, contributor guides, and agent process docs.

## License

Licensed under the Apache License, Version 2.0. See [LICENSE](LICENSE) for details.
