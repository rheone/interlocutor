# Pipeline Force-Eject + Mutable RequestContext.Items

> **Status: done** - both tickets below were designed and implemented in a single session; this PRD is a retroactive record for tracker traceability, not a forward plan.

## Background

Grew out of a design conversation about Interlocutor's cancellation handling. The user clarified that what they actually wanted was unrelated to `CancellationToken`/`Cancelled`/`CancellationBehavior`: a developer-initiated **"force eject"** - the ability to abandon a pipeline or handler from anywhere inside either (including deep in nested private-method calls within a handler body), immediately, with no further pipeline steps or transaction commit, returning whatever legal `IOperationOutcome` case the developer explicitly chooses.

Investigation during planning showed most of the desired behavior already worked for free:

- An ordinary `IPipelineBehavior` can already short-circuit by simply not calling `next()` and returning any `TResponse` directly (`AuthorizationBehavior` already does this for `Forbidden`).
- `UnitOfWorkBehavior` (EF Core and NHibernate) already decides commit-vs-rollback purely from `IOutcomeClassifier.IsFailure(result)` - fail-closed by default - so any non-success outcome a developer returns already triggers rollback, with no new transaction-gating logic needed.

The one real gap - no ergonomic way to return a chosen outcome from deep inside a handler's own call graph without manually threading the value back up through every intermediate method frame - became Ticket 01.

Separately, the user wanted a request/pipeline-scoped metadata bag - settable from the handler and from any pipeline behavior, readable later - extending the already-existing but unused `IRequestContextAccessor`/`RequestContext.Items` (previously `IReadOnlyDictionary<string,object>?`, never populated anywhere in the codebase, documented only as a whole-record `with`-replace). The user wanted it genuinely settable in place and "open to change for future needs." That became Ticket 02.

Three scope decisions were confirmed with the user directly before implementation:

1. `Items` becomes a mutable, thread-safe bag (accepted breaking change to its declared type - repo is pre-1.0).
2. The eject mechanism covers all 4 pipeline-composition sites (3 hand-written wrappers + the source generator's emitted-code template), not just the hand-written ones.
3. Per-DI-scope context granularity is acceptable (today's `AddScoped IRequestContextAccessor` lifetime) - no new AsyncLocal/per-dispatch machinery; the multi-`Send`-per-scope caveat is documented instead of solved with new machinery.

## Tickets

1. [Pipeline/handler force-eject](issues/01-pipeline-force-eject.md)
2. [Mutable, extensible RequestContext.Items](issues/02-mutable-request-context-items.md)

Independent of each other - neither blocks the other.
