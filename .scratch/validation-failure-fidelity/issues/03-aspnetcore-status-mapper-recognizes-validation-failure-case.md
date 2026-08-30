# 03 - `DefaultOutcomeStatusMapper` recognizes `IValidationFailureCase`

Status: not started

## Labels

ready-for-agent

## What to build

`DefaultOutcomeStatusMapper.Map<TFailure>` (`src/Interlocutor.AspNetCore/DefaultOutcomeStatusMapper.cs`) pattern-matches concrete case types in a `switch`, falling back to `MapUnrecognized`/`MapNotFoundGenericOrUnrecognized` for anything it doesn't know. Without a change here, `FluentValidationErrors` (ticket 02) silently falls into that unrecognized bucket instead of mapping to 422 the way `ValidationErrors` does today.

Add an `IValidationFailureCase v => this.MapValidationFailureCase(v)` branch, ordered **after** the existing exact `ValidationErrors validationErrors => this.MapValidationErrors(validationErrors)` branch so today's mapping for that concrete type is completely unaffected. `MapValidationFailureCase` is a new `protected virtual` method (matching the class's existing per-case-override pattern) mapping to `StatusCodes.Status422UnprocessableEntity` with a `ProblemDetails` body built from `IValidationFailureCase.Messages`.

`Interlocutor.AspNetCore` already depends on `Interlocutor.CommonKernel` (where `IValidationFailureCase` lives), so this needs no new package dependency and no dependency on `Interlocutor.FluentValidation` - this ticket can proceed independently of ticket 02.

## Acceptance criteria

- [ ] `FluentValidationErrors` maps to 422 Unprocessable Entity, not `MapUnrecognized`
- [ ] Existing `ValidationErrors` mapping is byte-for-byte unchanged (exact-type branch still wins)
- [ ] `MapValidationFailureCase` is `protected virtual`, consistent with every other case-mapping method on the class
- [ ] Tests cover: `FluentValidationErrors` mapping, `ValidationErrors` mapping unaffected, and a plain hand-rolled `IValidationFailureCase` implementer also mapping correctly (proving the interface-based recognition, not a type-specific special case)
- [ ] `dotnet build` and `dotnet test` remain clean

## Blocked by

01 - `IValidationFailureCase` interface must exist first. Not blocked by 02.
