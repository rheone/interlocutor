Status: needs-triage
Severity: MEDIUM
Category: documentation
Source: documentation.md lane, confirmed by antagonist lane with additional root-cause context

# `pipeline-and-behaviors.md` describes execution order using fictitious `OnIngress()`/`OnEgress()` methods

## Summary

The "Flat Ordered List" section narrates pipeline execution as calls to `Logging.OnIngress()`/`Transaction.OnEgress()` etc., but `IPipelineBehavior<TRequest, TResponse>` has no such members — only a single `Handle(request, next, ct)` method.

## Evidence

```
1. Inbound:  Logging.OnIngress() → Auditing.OnIngress() → Validation.OnIngress() → Transaction.OnIngress() → handler
2. Outbound: handler result flows back out; behaviors run their egress in reverse order: Transaction.OnEgress() → Validation.OnEgress() → Auditing.OnEgress() → Logging.OnEgress()
```

`rg -rn "OnIngress|OnEgress" src/` returns nothing. `src/Interlocutor.Abstractions/Pipeline/IPipelineBehavior.cs` defines exactly one member: `Task<TResponse> Handle(TRequest request, Func<Task<TResponse>> next, CancellationToken ct)`.

Root cause identified by the antagonist pass: there *is* a real before/after hook pair in shipped code — `IPipelineStage.OnBeforeAsync`/`OnAfterAsync`, consumed by `PipelineStageBehavior` — but that's a different, differently-named mechanism for a different type. `OnIngress`/`OnEgress` is leftover terminology specifically from `maintainer_docs/bootstrapreference/Conversation.md` (a superseded pre-implementation design doc that used those exact names for what became `PipelineStage`) — the doc section in question is actually describing `LoggingBehavior`/`AuditingBehavior`/`ValidationBehavior`/`TransactionBehavior`, none of which have any before/after hook at all, just `Handle`. The file's own later "Composition Diagram" and "Registering Custom Behaviors" sections already correctly describe the real `Handle`/`next()` shape — this is the one leftover inconsistent spot.

## Suggested action

Rewrite the two bullets to describe inbound/outbound flow in terms of the actual `Handle(request, next, ct)` contract (each behavior calling `next()` on the way in, then acting on the awaited result on the way out) instead of the `OnIngress`/`OnEgress` names inherited from the superseded design doc.
