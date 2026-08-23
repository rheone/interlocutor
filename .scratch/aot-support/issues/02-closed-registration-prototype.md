# 02 - Prototype: does closed-generic behavior registration survive real dispatch?

Type: prototype
Status: open
Blocked by: 01

## Question

Assuming the research says closed-generic registration lifts the value-type wall in principle, does it hold
up against this library's actual pipeline semantics when run end to end?

A closed registration per `(TRequest, TResponse)` pair is easy to state and easy to get subtly wrong. The
prototype exists to make the failure modes concrete rather than argued:

- **Constraint filtering.** Behaviors are currently scoped per request by generic constraints on marker
  interfaces (`ITransactionalRequest`, `IAuditableRequest`, `IRequiresPolicy`, `IExternalStoreRequest`).
  Open-generic resolution applies that filter naturally - a behavior whose constraint the request fails to
  satisfy simply is not resolved. A closed registration is emitted per pair by the generator, which must
  reproduce that filter at compile time. Does it, exactly?
- **Onion ordering.** First registered runs first inbound, last outbound
  ([ADR-0002](../../docs/adr/0002-pipeline-behavior-onion-model.md)). Does per-pair closed registration
  preserve relative order, including when the same behavior type is legitimately registered twice?
- **Fan-out.** `QueryHandlerWrapper` composes the chain from `services.GetServices<IPipelineBehavior<TQuery,
  TResponse>>()`. Does that enumeration return closed registrations in the same order, on both containers?
- **The stage host.** `PipelineStageBehavior` and the `AddStage`/`AddStageHost` family sit in the same chain.
  Do they need the same treatment?

Deliverable: a rough, throwaway prototype - hand-written closed registrations in the sample app are enough,
no generator work required - that either demonstrates all four hold, or names precisely which one breaks and
why. Link the prototype from this ticket rather than pasting it in.

If the research resolves all of this conclusively with evidence, close this as unnecessary rather than
performing it for form's sake.

## Scope reduction after Native AOT feasibility resolved

Kept open, but smaller. The research settled three of the four questions empirically against a genuine
native binary:

- **Fan-out and ordering: settled.** `GetServices<T>` over closed descriptors returns registration order and
  preserves duplicates, verified under both JIT and native.
- **End-to-end composition: settled.** Patched into this repo's own sample, the guard disappeared and
  `LoggingBehavior -> PipelineStageBehavior -> handler` composed correctly.
- **Constraint filtering: NOT settled for this repo.** The research argues closed registration *replaces*
  runtime filtering with compile-time filtering, which is strictly better - but the evidence is
  martinothamar/Mediator's implementation plus an inventory of machinery this repo already has
  (`GenericConstraintChecker.Satisfies`, `GenericConstraintSnapshotBuilder`). It was not demonstrated against
  this generator.

So what remains is narrower than originally written: does *this* generator's constraint machinery emit the
right catalog for the marker-interface-constrained behaviors, and does the stage host need the same
treatment? Everything else here is already answered - do not redo it.

One finding sharpens why this matters: under `IsDynamicCodeSupported=false` the current runtime filtering is
**already broken**, throwing for requests a constrained behavior was never meant to apply to. Compile-time
filtering is not just an optimization, it is a fix.

## Answer

_Pending._
