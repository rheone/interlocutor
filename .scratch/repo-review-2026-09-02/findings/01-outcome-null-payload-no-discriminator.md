Status: needs-triage
Severity: CRITICAL
Category: bug
Source: bugs.md lane, confirmed empirically by antagonist lane (independent throwaway test against the real compiled library)

# `Outcome` unions can't distinguish "success case holds null" from "no case set"

## Summary

Every `Outcome`/`Outcome<...>` union (arity 2-6) stores its active case as a single boxed `object Value` field with no discriminator/tag. A legitimately successful outcome whose case payload is `null` (a nullable reference type, or a `Nullable<T>` with `HasValue == false`) is bit-for-bit identical to an unset union. This is a silent, no-visible-exception data-loss bug: a successful write gets **rolled back** as if it failed.

## Evidence

Verified twice, independently, against the real compiled library (`net11.0-preview` SDK, `union`/`IUnion` feature) — not just by reading source:

```
Outcome<string?, NotFound> result = (string?)null;
result.IsSuccess                 -> False   (should be True — nothing failed)
result.TryGetValue(out string?)  -> False
result.TryGetValue(out NotFound) -> False
result.Match(...)                -> InvalidOperationException: "Outcome holds no recognized case value."
reflected instance fields        -> exactly one: Object <Value>k__BackingField
```

Root cause: `src/Interlocutor.Generator/OutcomeMemberEmitter.cs`'s `EmitIsSuccess`, `EmitMatch`/`EmitSwitch`, and `EmitTryGetValueOverloads` are all built on `this.Value is <caseType>` or `switch (this.Value) { <caseType> ... }` — C# type patterns, which **never match `null`**, regardless of the declared case type's nullability. Every arity 2 through 6 mis-handles a null-payload case identically.

## Blast radius

- `src/Interlocutor.Abstractions/Pipeline/Classifiers/OutcomeClassifierBuilder.cs`'s `DefaultOutcomeClassifier.IsFailure` unwraps via `IUnion.Value`; for a null-payload case nothing matches, so the documented fail-closed policy reports `IsFailure == true`.
- Both `src/Interlocutor.Persistence.EntityFrameworkCore/UnitOfWorkBehavior.cs` and `src/Interlocutor.Persistence.NHibernate/UnitOfWorkBehavior.cs` gate commit/rollback on exactly this classifier → **a successful write with a null success payload is rolled back instead of committed.**
- `AuditingBehavior`/`LoggingBehavior` use the same classifier, so the request is also logged/audited as a failure it never was.
- `Interlocutor.AspNetCore`'s hand-written switches (`ResultExtensions`) throw `NotImplementedException`.
- See [07-aspnetcore-null-case-mismapping.md](07-aspnetcore-null-case-mismapping.md) for a second, distinct blast radius in the reflective HTTP-mapping path.
- **Realistic trigger, not a contrived edge case:** `ResourceCommandHandler<TCommand, TEntity, TSuccess>` (the library's own built-in resource-authorization template) lets a consumer pick any `TSuccess`. A consumer who reasonably follows CLAUDE.md's own result-type convention and picks a nullable `TSuccess` (e.g. `string?` for an optional generated id) hits this bug through the library's own recommended entry point, not just hand-written `Outcome<...>` construction.

## Suggested action

Give the `union`-backed `Outcome` types a way to distinguish "case N set, value null" from "no case set" — e.g. box null case payloads into a private sentinel wrapper before storing, or use a real discriminator/tag if the preview `union` feature exposes one — and update `OutcomeMemberEmitter`'s generated `IsSuccess`/`TryGetValue`/`Match`/`Switch` to consult it.

Regression tests to add once fixed:
- `Interlocutor.CommonKernel.Tests`: a null-payload case for at least one arity, covering `IsSuccess`, `TryGetValue`, `Match`, `Switch`.
- `UnitOfWorkBehavior` transaction tests (both EF Core and NHibernate): a handler that succeeds with a null payload must **commit**, not roll back.
- A test routed through `ResourceCommandHandler` + `UnitOfWorkBehavior` together (not just bare `Outcome<...>` construction) — this is the realistic path a consumer following the library's documented patterns would hit it through.
- The `Interlocutor.AspNetCore` `ToActionResult()` path — see issue 07.

Until fixed, consider documenting the hazard prominently (README/CLAUDE.md/glossary) so consumers know not to use a nullable reference type or `Nullable<T>` as an `Outcome` case's payload type.
