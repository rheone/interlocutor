# PRD: Pipeline Behavior Case Fidelity

Status: open - issues 01-06 not yet started.

## Problem

`AuthorizationBehavior`, `CancellationBehavior`, `RateLimitedBehavior`, `TimeoutBehavior`, `ValidationBehavior`, and `FluentValidationErrorsBehavior` are all generic over `TResponse : IOutcome`, deliberately unconstrained beyond that (see each type's own XML doc remarks, and ADR-0020's "Alternatives Considered" - a fixed failure-case generic parameter was rejected because Microsoft.Extensions.DependencyInjection requires an open-generic implementation to declare exactly as many open type parameters as the service interface it's paired against). Each behavior short-circuits by constructing a specific case (`Forbidden`, `Cancelled`, `RateLimited`, a timeout case, `ValidationErrors`, `FluentValidationErrors`) reflectively via `IOutcomeCaseRegistry`, through the shared `OutcomeCaseRegistryExtensions.ConstructOrThrow` helper (built for exactly this purpose - see `.scratch/code-review-followups/issues/03-shared-outcome-case-construct-or-throw-helper.md`).

When a handler's chosen `TResponse` union doesn't declare the case a behavior attached to its request needs, `ConstructOrThrow` throws `InvalidOperationException` - correctly, as a fail-fast configuration-bug signal. But that signal only fires the first time a real request actually triggers the mismatch (e.g. the first real denial for `IRequiresPolicy` paired with a `TResponse` lacking `Forbidden`), not at build time. `TimeoutBehavior` is also the one behavior of the six with no test proving this fail-fast path actually works. Nothing catches the mismatch earlier, and nothing records the design reasoning for *why* the response type must never be reshaped to satisfy pipeline behaviors instead of domain need.

## Decision

Formalize the already-correct runtime behavior (fail fast, never degrade to a silent/null outcome, never force a case into an incompatible type) as a documented convention (ADR-0022), close the one confirmed test gap, and add the earliest-feasible compile-time signal (a generator analyzer diagnostic) plus its runtime-discovery-path counterpart (a startup validation extension) - without changing `TResponse`'s shape, `IOutcomeCaseRegistry`'s contract, or how any container resolves these behaviors.

Two alternatives were seriously considered and explicitly rejected, to be recorded in ADR-0022 rather than re-litigated:
- A static-abstract-interface constraint (`IConstructibleFrom<TResponse, TCase>`) forcing every response arity to prove at compile time it can hold every case any attached behavior might need - rejected because it couples a handler's domain-chosen response arity to whichever behaviors happen to be wired into its pipeline.
- Letting an unconstructible case resolve to a null/unset `Outcome` instead of throwing - rejected because it breaks the exhaustiveness guarantee `Match`/`Switch`/a compiler-checked `switch` are built on, turning a loud, named, developer-facing exception into a silent non-value.

See `documentation/extensibility.md` for the existing "Registering Custom Behaviors" section this work extends, and ADR-0004/ADR-0009/ADR-0013/ADR-0020 for the prior decisions this stays consistent with.

## Verification

- `dotnet build` and `dotnet test` - no regression to any of the six behaviors' existing tests or observed exception messages.
- The new analyzer diagnostic fires on a genuinely mismatched `(TRequest, TResponse)` pair in a test fixture and stays silent on a matched one.
- `documentation/extensibility.md`'s new section compiles its code example against the shipped API (house style truthfulness rule).
