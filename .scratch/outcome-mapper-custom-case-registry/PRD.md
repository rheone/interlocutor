# PRD: Extensible custom-failure-type dispatch for `OutcomeMapperBase<TResult>`

Status: **ready-for-agent**. Tracked as issues under `.scratch/outcome-mapper-custom-case-registry/issues/`.

## Problem Statement

A consumer deriving from `OutcomeMapperBase<TResult>` can already change how one *built-in* failure case maps - derive one level further than a concrete mapper (e.g. subclass `DefaultHttpOutcomeStatusMapper`) and override just that one protected method. But there is no way to add a dispatch rule for the consumer's *own* custom failure case type without it falling into the single `MapUnrecognized<TFailure>` catch-all alongside every other unrecognized type - losing the per-case-type mapping ergonomic every built-in case already gets.

## Solution

Add a small, optional registry to `OutcomeMapperBase<TResult>` - built the same way this codebase's two existing structurally identical problems are already solved (`Classifiers.OutcomeClassifierBuilder`, `Cases.OutcomeCaseRegistryBuilder`: a builder accumulating registrations into an immutable lookup, consulted with a fallback) - that a consumer populates with per-custom-type mapping rules. `MapUnrecognized<TFailure>`'s call site consults this registry first; a consumer's own `MapUnrecognized` override remains the fallback for anything the registry doesn't recognize. Every built-in case keeps dispatching through the existing abstract-method switch, completely unchanged - only the previously-single "anything else" bucket gains structure.

## User Stories

1. As a consumer deriving from `OutcomeMapperBase<TResult>`, I want to register a mapping rule for my own custom failure case type, so that it maps distinctly instead of landing in a single generic catch-all alongside every other unrecognized type.
2. As a consumer, I want every built-in failure case (`NotFound`, `Forbidden`, `Conflict`, `ValidationErrors`/`IValidationFailureCase`, `Failure`/`Failures`, `Cancelled`, `Timeout`, `RateLimited`, the generic `NotFound<TId>`) to keep dispatching exactly as it does today, so that adopting the registry costs nothing for a mapper that only handles built-in cases.
3. As a consumer, I want the compiler to still refuse to let me build a concrete `OutcomeMapperBase<TResult>` subclass that hasn't handled every built-in case, so that I can't silently ship a mapper with a forgotten built-in case.
4. As a consumer, I want to register a custom-type rule using the same builder shape I already use for `OutcomeClassifierBuilder`/`OutcomeCaseRegistryBuilder`, so that I'm not learning a third, different registration idiom for the same category of problem.
5. As a consumer, I want a later registration for a type to override an earlier one for that same type, so that composing registrations from more than one source behaves predictably.
6. As a consumer who registers no custom types, I want `MapUnrecognized<TFailure>`'s behavior to be unchanged from today (my own override still runs, unconditionally), so that adopting this feature is fully backward compatible.
7. As a maintainer of `Interlocutor.AspNetCore`'s `DefaultHttpOutcomeStatusMapper` (or a future non-HTTP mapper - gRPC, CLI, queue), I want the same registry mechanism available to me, so HTTP has no special status in this extension point, consistent with ADR-0021.
8. As a consumer, I want this to require no DI registration or container involvement, so it stays consistent with `OutcomeMapperBase<TResult>`'s existing "construct an instance, call `Map` directly" usage model.
9. As a consumer reading the generated `MapUnrecognized` call path, I want it obvious that the registry is consulted before my override runs, so I can reason about precedence without reading the base class's source.

## Implementation Decisions

- `OutcomeMapperBase<TResult>.Map<TFailure>`'s existing abstract-method-per-built-in-case `switch` is unchanged - this work does not touch the closed built-in set or its compile-time completeness guarantee.
- Add a new builder type in `Interlocutor.Abstractions.Pipeline.Mapping` (exact name decided during implementation - follow this codebase's existing naming pattern for `OutcomeClassifierBuilder`/`OutcomeCaseRegistryBuilder`) that accumulates `Type -> Func<TFailure, TResult>` registrations - a generic `RegisterType<TFailure>(Func<TFailure, TResult>)` plus a by-`Type`/`Delegate` overload, mirroring `OutcomeClassifierBuilder.RegisterType`'s two-overload shape - and builds an immutable lookup (matching the existing registries' `ImmutableDictionary`-backed storage).
- `OutcomeMapperBase<TResult>` gains a way to be constructed with (or supplied) one of these built registries; when none is supplied, behaves exactly as today.
- When `Map<TFailure>` falls through every built-in case and the reflective `NotFound<TId>` check also misses, consult the registry (exact-type lookup by `typeof(TFailure)`, no predicate or inheritance matching - matching the existing registries' exact-type-key dictionaries) before calling the subclass's `MapUnrecognized<TFailure>` override. A registry hit maps the value directly; a miss still falls through to `MapUnrecognized` unchanged.
- `IOutcomeMapper<TResult>`'s public interface shape (`TResult Map<TFailure>(TFailure failure)`) does not change - this is additive machinery inside `OutcomeMapperBase<TResult>` only.
- `DefaultHttpOutcomeStatusMapper` is not required to change by this work - it's a potential consumer of the new machinery, not something this PRD mandates be updated.

## Testing Decisions

- Test external behavior only: given a concrete `OutcomeMapperBase<TResult>` subclass with a populated custom-type registry, `Map<TFailure>` with a registered type returns the registered mapping; with an unregistered type it still reaches the subclass's `MapUnrecognized` override.
- Every existing built-in-case dispatch test for `OutcomeMapperBase<TResult>`/`DefaultHttpOutcomeStatusMapper` must continue passing unmodified - the regression signal that built-in dispatch is untouched.
- Prior art: `tests/Interlocutor.Abstractions.Tests` already covers `OutcomeClassifierBuilder`'s and `OutcomeCaseRegistryBuilder`'s registration/override/fallback behavior - mirror that test shape (register, override-by-re-register, fallback-when-unregistered) here.
- A test confirming a later registration for the same type overrides an earlier one, matching `OutcomeClassifierBuilder`'s documented last-write-wins behavior.

## Out of Scope

- Any change to built-in case dispatch (`MapNotFound`, `MapForbidden`, etc.) or to the generator's compile-time enforcement of the closed built-in set.
- Chain of responsibility, or any dispatch pattern other than the registry shape already established by `OutcomeClassifierBuilder`/`OutcomeCaseRegistryBuilder` - considered and rejected (see Further Notes).
- DI/container registration for mapper instances.
- Any change to `IOutcomeStatusMapper`/`DefaultHttpOutcomeStatusMapper` beyond what it inherits for free from `OutcomeMapperBase<TResult>`.

## Further Notes

- Chain of responsibility was the original idea floated for this problem; rejected in favor of this codebase's own existing precedent (`OutcomeClassifierBuilder`, `OutcomeCaseRegistryBuilder`) - introducing a third, novel dispatch pattern for a problem already solved twice the same way was judged worse than reusing the idiom.
- Independent of `.scratch/ioutcome-to-iunion-retirement/` - this work does not touch `IOutcome`, `IUnion`, or any response-shape/arity concept, only the failure-value-to-transport-representation mapping layer. Ships on its own schedule.
