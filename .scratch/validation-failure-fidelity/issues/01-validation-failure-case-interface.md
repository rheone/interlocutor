# 01 - `IValidationFailureCase` marker interface + ADR-0020

Status: not started

## Labels

ready-for-agent

## What to build

Add `IValidationFailureCase` to `Interlocutor.CommonKernel` (alongside `IOperationOutcome`, its existing marker-interface precedent) - a minimal, zero-dependency contract any validation-shaped failure case can implement:

```csharp
public interface IValidationFailureCase
{
    IReadOnlyList<string> Messages { get; }
}
```

Retrofit `ValidationErrors` (`src/Interlocutor.CommonKernel/ResultTypes/ValidationErrors.cs`) to implement it - it already exposes a `Messages`-shaped surface, so this should be a pure addition with no behavior change.

Write `maintainer_docs/adr/0020-validation-failure-case-fidelity.md`, following the shape of `maintainer_docs/adr/0019-per-request-type-pipeline-overrides.md` (Context/Decision/Alternatives Considered), cross-referencing ADR-0004 (marker interfaces for cross-cutting concerns) and ADR-0001 (C# unions for result types). Record the struct-not-class constraint (why this isn't a base-type-extended-per-technology design) and the decision to solve it via a shared minimal interface plus the union-arity case-type-choice convention.

This ticket is the foundation the rest of this feature (issues 02-04) builds on.

## Acceptance criteria

- [ ] `IValidationFailureCase` added to `Interlocutor.CommonKernel` with XML doc comments
- [ ] `ValidationErrors` implements `IValidationFailureCase` with no observable behavior change to any existing consumer
- [ ] `Interlocutor.CommonKernel/PublicAPI.Unshipped.txt` updated for the new interface and the `ValidationErrors` interface implementation
- [ ] Unit tests confirm `ValidationErrors` satisfies `IValidationFailureCase` and that `Messages` matches its existing message-exposure behavior
- [ ] `maintainer_docs/adr/0020-validation-failure-case-fidelity.md` written and accepted, cross-referencing ADR-0004 and ADR-0001
- [ ] `dotnet build` and `dotnet test` remain clean

## Blocked by

None - can start immediately.
