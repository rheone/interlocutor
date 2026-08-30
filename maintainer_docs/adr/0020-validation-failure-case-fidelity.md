# ADR-0020: Validation Failure-Case Fidelity

**Status:** Accepted

## Context

`Interlocutor.FluentValidation.ValidationBehavior` aggregates every registered `FluentValidation.IValidator<TRequest>`'s failures into a `CommonKernel.ValidationErrors`, but the aggregation keeps only each `ValidationFailure.ErrorMessage`. `FluentValidation.Results.ValidationFailure` carries substantially more than a message - `PropertyName`, `Severity`, `ErrorCode`, `CustomState`, `AttemptedValue` - all of it discarded before it ever reaches a handler's caller. A consumer who wants to render field-level errors, branch on severity, or round-trip a validator's `CustomState` has no way to get at that data through the pipeline today; the only escape hatch is bypassing `ValidationBehavior` entirely and re-validating by hand.

The same shape of problem exists in principle for any pipeline result case backed by a pluggable implementation (`RateLimited`, `Timeout`, `Forbidden`), but only validation has a live second data point (FluentValidation today; a hand-rolled or DataAnnotations-based validator as a realistic future one) motivating a fix now.

## Decision

`Interlocutor.CommonKernel`'s case types are `readonly record struct`s, and C# struct/record-struct has no inheritance - a `FluentValidationErrors : ValidationErrors` subtype is not just inadvisable, it does not compile. So this isn't a base-type-extended-per-technology design; it reuses the mechanism the library already has for "a handler picks exactly the failure shape it needs" ([ADR-0001](0001-use-csharp-15-unions-for-result-types.md)'s union-arity/case-type-choice convention), applied one level deeper - a technology-specific failure case type, not just a generic one.

### `IValidationFailureCase` (`Interlocutor.CommonKernel`)

A minimal, zero-dependency marker interface any validation-shaped failure case can implement, mirroring `IOperationOutcome` as CommonKernel's existing extension-point precedent and following the same marker-interface shape [ADR-0004](0004-marker-interfaces-for-cross-cutting.md) established for cross-cutting opt-in - except this marker is on a failure *case value*, not a request type:

```csharp
public interface IValidationFailureCase
{
    IReadOnlyList<string> Messages { get; }
}
```

`ValidationErrors` is retrofitted to implement it - no behavior change, since it already exposes a `Messages`-shaped surface.

`FluentValidationErrors` (`Interlocutor.FluentValidation`) is a new struct wrapping `IReadOnlyList<FluentValidation.Results.ValidationFailure>` verbatim (full fidelity), implementing `IValidationFailureCase`. It lives in `Interlocutor.FluentValidation`, not CommonKernel, because CommonKernel must never depend on FluentValidation's types. `FluentValidationErrorsBehavior<TRequest, TResponse>` has the same shape as `ValidationBehavior<TRequest, TResponse>` - aggregates every registered `FluentValidation.IValidator<TRequest>`, short-circuits when none are registered - but constructs `FluentValidationErrors` via `IOutcomeCaseRegistry` instead of `ValidationErrors`. This is purely additive: a handler opts in by declaring `FluentValidationErrors` as a case in its own `OperationOutcome<...>` union and registering this behavior instead of (or, if genuinely wanted, alongside - though not recommended) today's `ValidationBehavior`. Nothing about `ValidationBehavior` or `ValidationErrors` changes.

`Interlocutor.AspNetCore.DefaultOutcomeStatusMapper` gains an `IValidationFailureCase v => this.MapValidationFailureCase(v)` branch (ordered after the existing exact `ValidationErrors` branch, so today's mapping is unaffected) so `FluentValidationErrors` - and any future implementer - maps to 422 instead of falling into `MapUnrecognized`.

### Extension-point consistency for `RateLimited`/`Timeout`/`Forbidden`

Validation is an established extension point today (`Interlocutor.FluentValidation` swaps in for the built-in behavior); `RateLimited`/`Timeout`/`Forbidden` are architecturally the same shape - each backed by a swappable interface (`IRateLimiter`, `ITimeoutPolicy`, `IPolicyEvaluator`) - even though only one implementation of each ships today. Three things follow from treating them as the same kind of extension point, and they're deliberately split apart by cost and by who benefits:

- **The marker interface + retrofit** (`IRateLimitedFailureCase`, `ITimeoutFailureCase`, `IForbiddenFailureCase`, each implemented by the corresponding existing struct) is low-cost, non-breaking, and purely structural - added in the same change as `IValidationFailureCase`, so the pattern stays in sync across every result-type family and no CommonKernel retrofit is needed later.
- **`DefaultOutcomeStatusMapper` generic recognition of each interface** (a branch ordered after the exact-type match, exactly mirroring `IValidationFailureCase`'s branch) is likewise low-cost, shape-agnostic, and purely additive - it commits to no invented property shape, so it carries none of the risk the third bullet describes. Added proactively, without a first-party second implementation, specifically so a *consuming project's own* `IRateLimiter`/`ITimeoutPolicy`/`IPolicyEvaluator` implementation - not just a first-party one - maps correctly the moment it exists, with no `Interlocutor.AspNetCore` change required on the consumer's part.
- **A concrete second implementation's own case type and behavior** - the part with real design surface, an invented property shape, and no current driver - stays gated behind an explicit developer decision, parked as `needs-triage` notes until a real implementation (first-party or a consumer's own) is actually driving the shape. Building this speculatively would commit a public case-type shape nobody has validated against a real API, which is the kind of premature commitment this project's pre-1.0 norm (see ADR-0009, ADR-0015, ADR-0018) exists to avoid - the generic mapper branch above gets the forward-compatibility benefit without that risk.

The payoff of the first two bullets together: a consumer who implements one of these three interfaces themselves needs to write only their own case type and behavior - no change to this library at all, first-party or otherwise - exactly mirroring how `FluentValidationErrors` built on `IValidationFailureCase` for validation, minus the wait for a first-party second implementation to justify it.

## Alternatives Considered

- **Record-struct inheritance (`FluentValidationErrors : ValidationErrors`).** Rejected outright - does not compile. C# record structs cannot be inherited from.
- **Decompose `TResponse` into a fixed failure-case generic parameter on the behavior** (e.g. `ValidationBehavior<TRequest, TResponse, TFailure>`), so one generic behavior could construct whichever failure type a caller specifies. Rejected for the same reason `AuthorizationBehavior`'s and `ValidationBehavior`'s own XML docs already give for keeping `TResponse` unconstrained: Microsoft.Extensions.DependencyInjection requires an open-generic implementation to declare exactly as many open type parameters as the service interface it's paired against (`IPipelineBehavior<TRequest, TResponse>`, arity 2) - a third open parameter can't be left open behind that two-parameter contract.
- **A `TryConstruct` on `IOutcomeCaseRegistry` so one behavior multiplexes between `ValidationErrors` and `FluentValidationErrors` depending on which case `TResponse`'s union declares.** Deferred as unnecessary complexity for a first pass - shipping two distinct, explicitly-named behaviors and letting the consumer register the one matching their chosen case type is simpler, consistent with how every other custom-behavior swap in this library already works (register what you want instead of what you don't), and avoids adding a non-throwing construction path to `IOutcomeCaseRegistry` that nothing else needs yet.
- **Preemptively building a second implementation's own case type and behavior for `RateLimited`/`Timeout`/`Forbidden`.** Rejected for now - each has exactly one shipped implementation today, so there is nothing to be agnostic *between* yet, and any property shape chosen would be invented rather than validated against a real API. Parked as `needs-triage` notes rather than built speculatively. The marker interface and the mapper's generic recognition of it are a different call (see "Extension-point consistency" above) - both low-cost, shape-agnostic, and non-breaking enough to add now rather than parking them alongside the higher-surface work.

## Consequences

- **Positive:** A consumer who needs full `ValidationFailure` fidelity (field name, severity, error code, custom state, attempted value) can opt into it without any change to `ValidationBehavior`/`ValidationErrors` for consumers who don't need it.
- **Positive:** `DefaultOutcomeStatusMapper` recognizes any `IValidationFailureCase` implementer, not just the library's own concrete types, so a consumer's own hand-rolled validation failure case also maps to 422 without a mapper override.
- **Positive:** `IRateLimitedFailureCase`/`ITimeoutFailureCase`/`IForbiddenFailureCase`, together with `DefaultOutcomeStatusMapper`'s generic recognition of each, mean a future second implementation of any of those three families - first-party or a consuming project's own - needs no change to this library at all, only its own case type and behavior.
- **Negative:** Two behaviors now exist for validation (`ValidationBehavior`/`FluentValidationErrorsBehavior`), and a consumer must understand which one to register - mitigated by both being explicitly named for what they produce, rather than a shared name with different generic parameters.
