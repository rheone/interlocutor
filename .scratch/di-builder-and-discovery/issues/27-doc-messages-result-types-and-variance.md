> **Amendment** - this ticket instructed citing ADR-0001 and ADR-0013 directly in the shipped `documentation/messages-and-result-types.md` prose. That instruction is superseded: `documentation/` files must state design rationale as fact and must never cite or link to `docs/adr/`, `docs/PRD.md`, or other `docs/` paths - see the house convention added in ticket 36. The shipped file has been corrected by ticket 36. No action needed here; historical context only.

## Labels

ready-for-agent

## What to build

A `documentation/messages-and-result-types.md` covering requests, result types, and the variance rule. Primary sources: ADR-0001 (C# 15 unions for result modeling), ADR-0005 (void command pipeline support), ADR-0013 (response type constrained to the outcome interface; streaming explicitly excluded), `docs/PRD.md` §6 (type catalog) and §7 (CQRS interfaces).

Cover:

- **The message shapes**: `IQuery<TResponse>`, `ICommand<TResponse>`, `ICommand` (void), and their enumerable/bulk/paged variants where they exist in shipped code - verify the current set against `src/Interlocutor.Abstractions`, don't assume the bootstrap-PRD list (`IEnumerableCommand`, `IBulkCommand`, etc.) all shipped as originally sketched.
- **`OperationOutcome<...>` and its case types** (`Success`, `NotFound`, `Forbidden`, `Conflict`, `ValidationFailed`, `Failure`, ...) - use the case-type names resolved in ticket 21 against actual `Interlocutor.CommonKernel` source, not the older README/CONTEXT.md wording. Link to the full marker-interface catalog (`ITransactionalRequest`, `IExternalStoreRequest`, `IAuditableRequest`, `IRequiresPolicy`) briefly, pointing to tickets 23/30 for where each is used rather than re-explaining them here.
- **The existing worked-example guide** at `docs/guides/composing-operationoutcome-arities.md` - link to it for concrete arity examples rather than duplicating those examples here.
- **Variance**, stated plainly and marked `[!CAUTION]` per house convention as the single most likely thing a future contributor attempts and fails at: the response type parameter can never be variant. It sits inside a `Task` on the handler interfaces, and in *both* an argument position and the return position on the behavior interface. Polymorphism is request-side only, permanently, across every interface in the library.
- **The deliberate absence of streaming**, with the reasoning from ADR-0013, so it reads as a closed, considered decision rather than a gap - streaming and "commit/rollback as one unit" don't compose, which is why it's deliberately kept out of `OperationOutcome`.

## Acceptance criteria

- [ ] Message shapes are documented against the actual shipped `Interlocutor.Abstractions` types, not the bootstrap-PRD sketch
- [ ] `OperationOutcome` case types use names verified against current `Interlocutor.CommonKernel` source
- [ ] Existing guide at `docs/guides/composing-operationoutcome-arities.md` is linked, not duplicated
- [ ] Variance explanation states plainly that response-side variance is impossible and why, flagged with `[!CAUTION]`
- [ ] Streaming's deliberate absence is explained with its rationale, closing the question rather than leaving it open
- [ ] Every code example compiles against the shipped API
- [ ] Follows house conventions from ticket 21

## Blocked by

21 - Establish documentation/ directory; retire CONTEXT.md
