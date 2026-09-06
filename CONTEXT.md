# Interlocutor

Interlocutor is a mediator/CQRS pipeline library for .NET. Handlers return `Outcome` unions — compiler-checked result types — rather than throwing for expected failures; exceptions are reserved for bugs and infrastructure faults.

## Language

**Arity**:
The count of type parameters a consumer supplies to a handler response's `Outcome` shape.
- **0** - the non-generic `Outcome`: a pure fire-and-forget acknowledgment. No cases, no payload, no success/failure judgment of any kind - just confirmation the operation ran. Usable only by a request that is genuinely infallible and opts into no short-circuiting pipeline behavior (`ICancellable`/`IHasTimeout`/`IRateLimited`), since none of those has a case to construct into a case-less type.
- **1** - `Outcome<T>`: a single always-succeeding payload wrapper - a "fake union." Not a real discriminated union (there's nothing to distinguish), so it isn't generator-emitted, but it exposes the same member shape as every other arity (`Value`, `Match`, `Switch`, `TryGetValue`, equality, `ToString`) by hand, so a caller never has to special-case it.
- **2 to 16** - `Outcome<T1, ..., TN>`: a real discriminated union, generator-emitted. Every case type is named symmetrically (`T1..TN`, never `TSuccessResult`/`TFailureResult`) and chosen entirely by the consumer - including whether any of them plays a "success" or "failure" role. There is no privileged, pre-baked `Outcome<Success, Failure>` - a developer choosing a binary outcome picks whichever two types the operation actually produces; `Success`/`Failure` are ordinary case types available for any slot, with no special claim on arity 2.
_Avoid_: "Result shape" for this concept - "arity" is the term used throughout `documentation/` and `maintainer_docs/`.
