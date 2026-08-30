# 02 - `FluentValidationErrors` case type + `FluentValidationErrorsBehavior`

Status: done

## Labels

ready-for-agent

## What to build

In `Interlocutor.FluentValidation`, add:

- **`FluentValidationErrors`** - a `readonly record struct` wrapping `IReadOnlyList<FluentValidation.Results.ValidationFailure>` verbatim (no lossy projection - `PropertyName`, `Severity`, `ErrorCode`, `CustomState`, `AttemptedValue` all preserved), implementing `IValidationFailureCase` (from ticket 01) via a computed `Messages` (each failure's `ErrorMessage`).
- **`FluentValidationErrorsBehavior<TRequest, TResponse>`** - same structure as the existing `ValidationBehavior<TRequest, TResponse>` (`src/Interlocutor.FluentValidation/ValidationBehavior.cs`): aggregates every registered `FluentValidation.IValidator<TRequest>`, short-circuits to `next()` unchanged when no validators are registered or none report failures, otherwise short-circuits with a failure response - but constructs `FluentValidationErrors` via `IOutcomeCaseRegistry.ConstructOrThrow<TResponse, FluentValidationErrors>` instead of `ValidationErrors`.

This is strictly additive - `ValidationBehavior` and `ValidationErrors` are not modified, deprecated, or touched. A consumer opts into full fidelity by declaring `FluentValidationErrors` as a case in their own `OperationOutcome<...>` union and registering `FluentValidationErrorsBehavior` (via `AddBehavior(typeof(FluentValidationErrorsBehavior<,>))`) instead of `ValidationBehavior`.

## Acceptance criteria

- [x] `FluentValidationErrors` preserves every field of every `ValidationFailure` from every registered validator - no data collapsed to `.ErrorMessage` only
- [x] `FluentValidationErrorsBehavior` aggregates failures across every registered validator, matching `ValidationBehavior`'s existing "preserve every violation from every validator" guarantee
- [x] `FluentValidationErrorsBehavior` short-circuits to `next()` unchanged when no validators are registered, matching `ValidationBehavior`'s existing behavior
- [x] `Interlocutor.FluentValidation/PublicAPI.Unshipped.txt` updated for both new types
- [x] New unit tests mirror the existing `ValidationBehavior` test suite's shape and coverage for the new behavior
- [x] Existing `ValidationBehavior`/`ValidationErrors` tests remain green and unmodified
- [x] `dotnet build` and `dotnet test` remain clean

## Blocked by

01 - `IValidationFailureCase` interface must exist first.
