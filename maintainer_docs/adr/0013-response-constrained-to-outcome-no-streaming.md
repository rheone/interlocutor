# ADR-0013: Response Type Constrained to the Outcome Interface; Streaming Excluded

**Status:** Accepted

## Context

The library's central convention - every query and command returns a compiler-checked outcome union rather than throwing for expected failures - is documented, but nothing in the type system enforces it. A query or command author can today declare a response type that is a bare DTO with no `IOutcome` shape at all, and nothing catches it until a consumer notices the handler can't express failure. The unit-command dispatch overload already constrains its result type parameter to `IOutcome` (see [ADR-0005](0005-void-command-pipeline-support.md)); the typed query/command path does not carry the equivalent constraint.

Separately, a streaming response - an `IAsyncEnumerable<T>` or similar incremental result - has come up repeatedly as a plausible future request, and needs a settled answer rather than being silently unaddressed.

## Decision

- **Query and command request interfaces gain a constraint requiring their response type to implement the base outcome interface (`IOutcome`).** This mirrors the constraint the unit-command dispatch overload already carries. It is breaking for any consumer currently returning a bare DTO as a response type - pre-1.0, no shim; that consumer's response type must become (or be wrapped in) an outcome union.
- **Streaming is out of scope, permanently, not merely unaddressed.** Handlers return materialized results. There is no streaming request abstraction and no async-sequence response type. A consumer needing paged or incremental data defines their own paged type as a union payload - paged result types are documentation illustrations, not a shipped type (see the notes on `PagedResult<T>` in the glossary).
- **Variance stays request-side only, permanently, and this constraint is why it must.** The response type parameter appears inside a `Task<TResponse>` on the handler interfaces, and in *both* an argument position (`Func<Task<TResponse>> next`) and the return position on `IPipelineBehavior<TRequest,TResponse>.Handle`. A type parameter that appears in both an `in` and an `out` position on the same interface can never be marked variant - C# would reject the declaration. This is not a limitation the constraint introduces; it is a fact about the shape the pipeline already had, restated here because it is the single most likely thing a future contributor attempts and fails at. Handler interfaces are already contravariant on the request type today (see [ADR-0004](0004-marker-interfaces-for-cross-cutting.md) and the notification fan-out decision in [ADR-0010](0010-dispatch-scope-fan-out-open-generics-ambiguity.md)); what changes here is a compile-time guarantee on the response side, not an attempt at response-side variance.

## Consequences

- **Positive:** "Every response is an outcome" moves from a documented convention to a compiler-enforced one - a bare DTO response type fails to compile against `IQuery<TResponse>`/`ICommand<TResponse>`.
- **Positive:** Runtime-typed dispatch (see [ADR-0010](0010-dispatch-scope-fan-out-open-generics-ambiguity.md)) can rely on every response implementing `IOutcome`, which is what makes a single non-generic runtime dispatch return type possible at all.
- **Positive:** Closing the streaming question permanently means it stops being re-litigated per request type; a consumer needing incremental delivery knows to look elsewhere (a message bus, gRPC streaming, gathered outside the mediator) rather than waiting on a future version.
- **Negative:** Breaking change for any existing consumer with a bare-DTO response type. Accepted - pre-1.0, no migration shims ship for this effort (see [ADR-0008](0008-compile-time-handler-discovery.md)'s note that every prior registration overload is deleted the same way).

## Alternatives considered

- **Leave the response type unconstrained, rely on documentation:** Rejected - this is exactly the gap this decision closes; an undocumented DTO response type currently compiles and only fails a human code review, not the build.
- **Support streaming via `IAsyncEnumerable<T>` alongside materialized results:** Rejected - a streamed sequence cannot itself be wrapped in a single `IOutcome` the way a materialized value can (failure could occur mid-stream, after some items were already yielded), which would force a second, incompatible outcome-reporting model alongside the one this library is built around. Out of scope for this effort, not merely deferred.
- **Introduce response-side covariance via a non-generic base outcome return, sacrificing the typed response:** Rejected - would trade away the compiler-checked exhaustive `switch` at the call site, the entire reason the union-typed result model exists (see [ADR-0001](0001-use-csharp-15-unions-for-result-types.md)).
