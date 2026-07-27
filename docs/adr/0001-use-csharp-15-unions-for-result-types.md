# ADR-0001: Use C# 15 Union Types for Result Modeling

**Status:** Accepted

## Context

Handlers need to communicate a closed set of expected outcomes — a success value or one of several named failures — in a way the compiler can check exhaustively. Prior options — `object` returns, boolean flags with out parameters, third-party libraries like `OneOf`, or throwing exceptions for expected outcomes — each fail at least one of: compile-time exhaustiveness, discoverability from the signature, or freedom from an external dependency.

## Decision

Use the C# 15 `readonly union` keyword to declare `Result<T>`, `Result`, and `Failure` as closed unions of record case types. All union declarations are isolated to `Mediance.CommonKernel/Results.cs`.

```csharp
readonly union Failure(ValidationFailed, RuleViolation, NotFound, Forbidden, Conflict);
readonly union Result<T>(T, Failure);
readonly union Result(Success, Failure);
```

Failure cases are sealed records:

- `ValidationFailed` — map of field names to `(Code, Message)` lists, produced only by `ValidationBehavior`
- `RuleViolation` — business rule violation, produced by handler code
- `NotFound` — entity not found, produced by `ResourceCommandHandler.LoadAsync` or handler code
- `Forbidden` — authorization denied, reason for server-side logging only
- `Conflict` — state conflict (concurrency, uniqueness, invalid transition)

## Consequences

- **Positive:** Compiler-enforced exhaustive `switch` at every consumption site; adding a new failure case surfaces every place that needs updating.
- **Positive:** No external dependency — replaces what `OneOf` would have provided.
- **Negative:** Language surface is preview-only; syntax may shift before .NET 11 GA (~November 2026). Mitigated by isolating all `union` declarations to a single file.
- **Negative:** Compiler-generated unions are structs that box their `Value` — acceptable for a mediator layer; flagged as a future optimization point.

## Alternatives considered

- **OneOf (NuGet):** Runtime-checked, not compiler-exhaustive. Dropped once native unions became available in preview.
- **Custom sealed abstract record hierarchy:** Works without preview features but requires shared base type and loses "any type can be a case" flexibility. Fallback if union syntax churns too much before GA.
- **Exceptions for expected outcomes:** Rejected — violates the core design goal of making outcomes visible in type signatures.
