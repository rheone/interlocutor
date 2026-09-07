# PRD: Validation Failure-Case Fidelity

Status: done - issues 01-04 and 08 implemented in 8a7ca91, recorded as ADR-0020. Issues 05-07 split by triage: each ticket's generic-mapper-recognition half is now also done (see its own file); the concrete-second-implementation half stays parked as needs-triage by design (no second implementation driving it yet). Issues 09-11 hold candidate shapes for that parked half, sanity-checked via a throwaway prototype (branch `prototype/failure-case-shapes`, commit `1dc9e50`) - also held at needs-triage, each pending its own maintainer decision (a named real implementation, plus a design call on `Timeout`'s `Elapsed` ambiguity and `Forbidden`'s `EvaluationOrder` question).

## Problem

`Interlocutor.FluentValidation.ValidationBehavior` aggregates every registered `FluentValidation.IValidator<TRequest>`'s failures into a `CommonKernel.ValidationErrors`, but the aggregation keeps only `ValidationFailure.ErrorMessage`:

```csharp
foreach (var error in result.Errors)
{
    errorMessages.Add(error.ErrorMessage);
}
```

`FluentValidation.Results.ValidationFailure` carries substantially more than a message - `PropertyName`, `Severity`, `ErrorCode`, `CustomState`, `AttemptedValue` - all of it discarded before it ever reaches a handler's caller. A consumer who wants to render field-level errors, branch on severity, or round-trip a validator's `CustomState` has no way to get at that data through the pipeline today; the only escape hatch is bypassing `ValidationBehavior` entirely and re-validating by hand.

The same shape of problem exists in principle for any pipeline result case backed by a pluggable implementation (`RateLimited`, `Timeout`, `Forbidden`), but only Validation has a live second data point (FluentValidation today; a hand-rolled or DataAnnotations-based validator as a realistic future one) motivating a fix now.

## Decision

`Interlocutor.CommonKernel`'s case types are `readonly record struct`s, and C# struct/record-struct has no inheritance - a `FluentValidationErrors : ValidationErrors` subtype is not just inadvisable, it does not compile. So this isn't a base-type-extended-per-technology design; it reuses the mechanism the library already has for "a handler picks exactly the failure shape it needs": the `OperationOutcome<TSuccess, ...>` union-arity/case-type-choice convention (see README § "Result Types Are Building Blocks"), applied one level deeper - a technology-specific failure *case type*, not just a generic one.

### `IValidationFailureCase` (`Interlocutor.CommonKernel`)

A minimal, zero-dependency marker interface any validation-shaped failure case can implement, mirroring `IOperationOutcome` as CommonKernel's existing extension-point precedent:

```csharp
public interface IValidationFailureCase
{
    IReadOnlyList<string> Messages { get; }
}
```

`ValidationErrors` is retrofitted to implement it - no behavior change, since it already exposes a `Messages`-shaped surface.

### `FluentValidationErrors` (`Interlocutor.FluentValidation`)

A new struct wrapping `IReadOnlyList<FluentValidation.Results.ValidationFailure>` verbatim (full fidelity), implementing `IValidationFailureCase`. Lives in `Interlocutor.FluentValidation`, not CommonKernel, because CommonKernel must never depend on FluentValidation's types.

### `FluentValidationErrorsBehavior<TRequest, TResponse>` (`Interlocutor.FluentValidation`)

Same shape as today's `ValidationBehavior<TRequest, TResponse>` - aggregates every registered `FluentValidation.IValidator<TRequest>`, short-circuits when none are registered - but constructs `FluentValidationErrors` via `IOutcomeCaseRegistry` instead of `ValidationErrors`. Purely additive: a handler opts in by declaring `FluentValidationErrors` as a case in its own `OperationOutcome<...>` union and registering this behavior instead of (or, if genuinely wanted, alongside - though not recommended, see the earlier "two independent behaviors" discussion) today's `ValidationBehavior`. Nothing about `ValidationBehavior` or `ValidationErrors` changes.

### `DefaultOutcomeStatusMapper` (`Interlocutor.AspNetCore`)

Gains an `IValidationFailureCase v => this.MapValidationFailureCase(v)` branch (ordered after the existing exact `ValidationErrors` branch, so today's mapping is unaffected) so `FluentValidationErrors` - and any future implementer - maps to 422 instead of falling into `MapUnrecognized`. `Interlocutor.AspNetCore` already depends on CommonKernel, so this needs no new package dependency.

### Extension-point consistency for `RateLimited`/`Timeout`/`Forbidden`

Validation is an established extension point today (`Interlocutor.FluentValidation` swaps in for the built-in behavior); `RateLimited`/`Timeout`/`Forbidden` are architecturally the same shape - each backed by a swappable interface (`IRateLimiter`, `ITimeoutPolicy`, `IPolicyEvaluator`) - even though only one implementation of each ships today. Three things follow from treating them as the same kind of extension point, split apart by cost and by who benefits:

- **The marker interface + retrofit** (`IRateLimitedFailureCase`, `ITimeoutFailureCase`, `IForbiddenFailureCase`, each implemented by the corresponding existing struct) is low-cost, non-breaking, and purely structural - done now, in issue 08, so the pattern stays in sync across every result-type family and no CommonKernel retrofit is needed later.
- **`DefaultOutcomeStatusMapper` generic recognition of each interface** - a branch ordered after the exact-type match, mirroring `IValidationFailureCase`'s branch - is likewise low-cost, shape-agnostic, and purely additive. Done now (see issues 05-07, each rescoped to just this half), specifically so a *consuming project's own* implementation of `IRateLimiter`/`ITimeoutPolicy`/`IPolicyEvaluator` maps correctly the moment it exists, with no library change required on the consumer's part.
- **A concrete second implementation's own case type and behavior** - the part with real design surface, an invented property shape, and no current driver - stays gated behind an explicit developer decision (issues 05-07's remaining scope, `needs-triage`).

## Alternatives Considered

- **Record-struct inheritance (`FluentValidationErrors : ValidationErrors`).** Rejected outright - does not compile. C# record structs cannot be inherited from.
- **Decompose `TResponse` into a fixed failure-case generic parameter on the behavior** (e.g. `ValidationBehavior<TRequest, TResponse, TFailure>`), so one generic behavior could construct whichever failure type a caller specifies. Rejected for the same reason `AuthorizationBehavior`'s and today's `ValidationBehavior`'s own XML docs already give for keeping `TResponse` unconstrained: Microsoft.Extensions.DependencyInjection requires an open-generic implementation to declare exactly as many open type parameters as the service interface it's paired against (`IPipelineBehavior<TRequest, TResponse>`, arity 2) - a third open parameter can't be left open behind that two-parameter contract.
- **A `TryConstruct` on `IOutcomeCaseRegistry` so one behavior multiplexes between `ValidationErrors` and `FluentValidationErrors` depending on which case `TResponse`'s union declares.** Deferred as unnecessary complexity for a first pass - shipping two distinct, explicitly-named behaviors and letting the consumer register the one matching their chosen case type is simpler, consistent with how every other custom-behavior swap in this library already works (register what you want instead of what you don't), and avoids adding a non-throwing construction path to `IOutcomeCaseRegistry` that nothing else needs yet.
- **Preemptively building a second implementation's own case type and behavior for `RateLimited`/`Timeout`/`Forbidden`.** Rejected for now - each has exactly one shipped implementation today, so there is nothing to be agnostic *between* yet, and any property shape chosen would be invented rather than validated against a real API. Parked as `needs-triage` notes (issues 05-07's remaining scope) rather than built speculatively. The marker interface and the mapper's generic recognition of it are a different call (see "Extension-point consistency" above) - both low-cost, shape-agnostic, and non-breaking enough to add now rather than parking them alongside the higher-surface work.

## Verification

- Full solution build and test run (`dotnet build`, `dotnet test`) - no regression to existing `ValidationBehavior`/`ValidationErrors`/`DefaultOutcomeStatusMapper` behavior or tests.
- New coverage proving `FluentValidationErrorsBehavior` preserves full `ValidationFailure` fidelity end-to-end (property name, severity, error code, custom state, attempted value all reach the caller).
- New coverage proving `DefaultOutcomeStatusMapper` maps `FluentValidationErrors` to 422 without changing `ValidationErrors`'s existing mapping.
- `documentation/` examples verified to compile against the shipped API (house style truthfulness rule).
