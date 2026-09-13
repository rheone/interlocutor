# ADR-0027: `ResourceCommandHandler` Ejects `NotFound`/`Forbidden` via `Eject.WithCase`, Retiring Its Fixed `TSuccess` Shape

**Status:** Accepted

## Context

`ResourceCommandHandler<TCommand, TEntity, TSuccess>` used to fix its response to
`Outcome<TSuccess, NotFound, Forbidden>` and construct `NotFound`/`Forbidden` directly at its two short-circuit
points (a missing entity from `LoadAsync`, a denied `Authorize` check). `TSuccess` was the last shipped generic
parameter anywhere in the library still named after a privileged "success" role -
`.scratch/outcome-naming-and-arity-refactor/issue 08` renamed every other case-type parameter across
`Outcome<...>`'s six shipped arities to a symmetric `T1..TN` scheme, but deliberately left
`ResourceCommandHandler<TCommand, TEntity, TSuccess>` and `AuthorizationBehavior<TRequest, TSuccess>` untouched,
on the grounds that each named "a real, contractual success payload for that specific construct, not a
positional union-case template." `AuthorizationBehavior`'s half of that carve-out closed separately, well before
this ADR, when it moved to `AuthorizationBehavior<TRequest, TResponse>` (see [ADR-0020](0020-validation-failure-case-fidelity.md)
and [ADR-0023](0023-pipeline-behavior-case-construction-fail-fast.md)) - `ResourceCommandHandler` was the one
remaining holdout.

[ADR-0025](0025-eject-withcase-type-erased-variant.md) introduced `Eject.WithCase<TCase>` for exactly this kind
of fixed-shape problem, but proved it only against `AuthorizationBehavior` - an `IPipelineBehavior<TRequest,
TResponse>` running inside the mediator's own behavior chain, where the chain's wrapper
(`QueryHandlerWrapper`/`CommandHandlerWrapper`/the generator's emitted equivalent) is the catch site that
already knows `TResponse` concretely. `ResourceCommandHandler` is not a pipeline participant - it is a
template-method base class a concrete handler inherits from and separately declares as
`ICommandHandler<TCommand, TResponse>` (see `documentation/sample-app-walkthrough.md`'s note on why that
explicit interface declaration is required for handler discovery). Whether an exception `Eject.WithCase` throws
from inside this class's `HandleAsync` actually reaches the same wrapper-side catch, given that indirection, or
needs a catch point of its own, was an open question - not something to assume from `AuthorizationBehavior`'s
proof alone.

`.scratch/resource-command-handler-case-fidelity/issues/01-spike-eject-withcase-outside-pipeline.md` spiked the
answer for real rather than estimating it: a prototype `ResourceCommandHandler<TCommand, TEntity, TResponse>`,
generic over `TResponse : IUnion`, ejecting `NotFound`/`Forbidden` via `Eject.WithCase` from both short-circuit
points, with `UpdateTodoTaskHandler` (`TutorialProject/TodoTasks.Api/Features/Tasks/UpdateTodoTask/UpdateTodoTaskHandler.cs`)
migrated as the worked example. That spike shipped in commit `be5adbb` ("feat: ResourceCommandHandler ejects
NotFound/Forbidden via Eject.WithCase"), and this ADR records the decision it reached - a spike answering "does
the mechanism reach" is not itself an acceptance decision, per this effort's own PRD, which is why this ADR
exists as a separate step.

## Decision

### Generic over `TResponse : IUnion`, ejecting rather than constructing

`ResourceCommandHandler<TCommand, TEntity, TResponse>` (`src/Interlocutor.Abstractions/Commands/ResourceCommandHandler.cs`)
is generic over `TResponse : IUnion` - the same unconstrained-beyond-`IUnion` shape every pipeline behavior and
every handler response already uses ([ADR-0013](0013-response-constrained-to-outcome-no-streaming.md)) - instead
of the fixed `Outcome<TSuccess, NotFound, Forbidden>` shape. Its `HandleAsync(TCommand, CancellationToken)`
template method ejects instead of constructing at both short-circuit points:

```csharp
if (entity is null)
{
    throw Eject.WithCase(new NotFound($"{typeof(TEntity).Name} '{this.DescribeKey(command)}' was not found."));
}

var authorizationResult = this.Authorize(entity, command);

if (authorizationResult is Forbidden forbidden)
{
    throw Eject.WithCase(forbidden);
}
```

This class takes no `IOutcomeCaseRegistry` dependency of its own, exactly like `AuthorizationBehavior` after its
ADR-0025 migration - it doesn't need to know how to construct a `TResponse` that carries `NotFound`/`Forbidden`,
only that `Eject.WithCase` will get one constructed downstream. A subclass is free to declare any `TResponse`
that has both a `NotFound` and a `Forbidden` case among its cases - including an arity wider than three, e.g. to
add a domain-specific `Conflict` alongside them - without abandoning this base class the way a handler with a
genuinely wider response shape (`TutorialProject`'s `CompleteTodoTaskCommand`, four cases) previously had to.

### The open question, answered: the existing wrapper-side catch already covers this path

**No new catch point was needed.** `ResourceCommandHandler` is a template-method base class, not a pipeline
behavior, but a concrete subclass is still registered and dispatched as an ordinary `ICommandHandler<TCommand,
TResponse>`. The mediator's `CommandHandlerWrapper` resolves that handler and hands its `HandleAsync` method -
which, for a subclass, resolves to this base class's public `HandleAsync(TCommand, CancellationToken)` - to
`PipelineChainExecutor.ExecuteAsync` as the innermost delegate. That executor already wraps the innermost
delegate call in a `try`/`catch (PipelineEjectCaseException)` before any `IPipelineBehavior<,>` runs (the same
guard [ADR-0025](0025-eject-withcase-type-erased-variant.md) added for `AuthorizationBehavior`), so an eject
thrown from `ResourceCommandHandler.HandleAsync` unwinds no further than that same catch regardless of the
template-method indirection - the executor is generic over the innermost delegate's `TResponse` and does not
care whether that delegate belongs to a behavior or a template-method base class.

This was proved empirically, not just argued from code reading: `ResourceCommandHandlerEjectCaseEndToEndTests`
(`tests/Interlocutor.Abstractions.Tests/ResourceCommandHandlerEjectCaseEndToEndTests.cs`) dispatches through a
real `Mediator` with **zero pipeline behaviors registered** - deliberately, unlike
`AuthorizationBehaviorEjectCaseEndToEndTests`, which needs `AuthorizationBehavior` in the chain to have something
to eject from. With no behavior in the chain, the wrapper/executor path is the only thing that could possibly
catch the eject, and the tests observe ordinary constructed `NotFound`/`Forbidden`/success responses back at the
`SendAsync` caller, not a leaked `PipelineEjectCaseException`.

No change to the dispatch/wrapper machinery itself was needed to make this reach: `PipelineChainExecutor`'s
existing catch, originally built for `AuthorizationBehavior`, was already general enough.

### Real behavioral consequence: a direct `HandleAsync` call outside the mediator now throws

Because `TResponse` is no longer known to `ResourceCommandHandler` itself, a subclass's short-circuit paths
(not-found/forbidden) can no longer be observed as a plain return value from a direct `HandleAsync` call that
bypasses the mediator - such a call now throws `PipelineEjectCaseException` instead of returning a constructed
case. This is a real, permanent behavioral change for any direct/unit-style caller of a `ResourceCommandHandler`
subclass, not just an internal implementation detail: a caller that previously did
`var result = await handler.HandleAsync(command, ct);` and pattern-matched the returned `Outcome<...>` for
`NotFound`/`Forbidden` must now either dispatch through a real `ISender`/`Mediator` (so the wrapper/executor
catch applies and a plain value comes back out) or assert the thrown `PipelineEjectCaseException` directly.
`ResourceCommandHandlerTests` (`tests/Interlocutor.Abstractions.Tests/ResourceCommandHandlerTests.cs`) and
`UpdateTodoTaskHandlerTests` (`TutorialProject/TodoTasks.Api.Tests/Features/Tasks/UpdateTodoTask/UpdateTodoTaskHandlerTests.cs`)
were both rewritten this way as part of `be5adbb`. This trade-off is accepted, not overlooked - see
Consequences below.

### Consistency with ADR-0023 and ADR-0025

The fail-fast guarantee [ADR-0023](0023-pipeline-behavior-case-construction-fail-fast.md) requires is unchanged:
a `TResponse` that can't hold `NotFound` or `Forbidden` still produces a named `InvalidOperationException` at
`PipelineChainExecutor`'s catch site via `OutcomeCaseRegistryExtensions.ConstructOrThrow`, never a silent or
reshaped outcome - `ResourceCommandHandler` inherits this for free from the same shared catch site
`AuthorizationBehavior` uses, without adding any fail-fast logic of its own.

This is the same `Eject.WithCase<TCase>` mechanism [ADR-0025](0025-eject-withcase-type-erased-variant.md)
introduced, now reaching a **second, non-behavior consumer**. ADR-0025 proved the mechanism against one
`IPipelineBehavior<TRequest, TResponse>` inside the chain, where the wrapper already knew it needed a catch
point because the wrapper *is* the chain's entry point. `ResourceCommandHandler` is not itself a chain
participant - it is the innermost delegate the chain wraps - and the fact that `PipelineChainExecutor`'s catch
sits around *that* delegate call too, not only around each behavior's invocation of `next()`, is what this ADR
adds to the record: the mechanism generalizes to "anything that is the innermost `HandleAsync` the wrapper
resolves and calls," not narrowly to "anything registered as an `IPipelineBehavior<,>`."

### Closing out the `TSuccess` naming carve-out

`.scratch/outcome-naming-and-arity-refactor/issue 08` named `ResourceCommandHandler<TCommand, TEntity, TSuccess>`
and `AuthorizationBehavior<TRequest, TSuccess>` as the two constructs deliberately kept out of the `T1..TN`
rename, on the grounds that each named "a real, contractual success payload for that specific construct." That
reasoning depended on the response shape actually being fixed enough for one slot to mean "the success case" -
once `TResponse` is generic over `IUnion` with no case privileged over any other (mirroring every `Outcome<...>`
arity and every pipeline behavior already renamed away from `TSuccess`), the premise for the carve-out no longer
holds. `AuthorizationBehavior`'s half of the carve-out closed earlier, alongside its own `Eject.WithCase`
migration; this ADR closes the `ResourceCommandHandler` half - `TSuccess` is renamed to `TResponse`, and no
shipped generic parameter anywhere in the library names a case after a privileged "success" role any longer.

## Consequences

- **Positive:** `ResourceCommandHandler` no longer fixes its response arity - a subclass may declare any
  `TResponse : IUnion` that carries `NotFound` and `Forbidden` among its cases, including a wider arity for a
  domain-specific case, without abandoning the base class.
- **Positive:** `ResourceCommandHandler` needs no `IOutcomeCaseRegistry` dependency and constructs nothing itself
  - construction is fully delegated to the same shared `PipelineChainExecutor` catch site `AuthorizationBehavior`
    already uses, with no new machinery added to reach it.
- **Positive:** The last shipped generic parameter named after a privileged "success" role is gone;
  `.scratch/outcome-naming-and-arity-refactor/issue 08`'s carve-out is fully closed rather than half-closed.
- **Negative, accepted permanently:** a direct/unit-style call to a `ResourceCommandHandler` subclass's
  `HandleAsync` that bypasses the mediator no longer observes `NotFound`/`Forbidden` as a returned value - it
  throws `PipelineEjectCaseException` instead. Any consumer test or infrastructure code written against the old
  return-value contract must be rewritten to dispatch through a real `ISender`/`Mediator`, or to assert the
  thrown exception directly, exactly as `ResourceCommandHandlerTests` and `UpdateTodoTaskHandlerTests` were.
- **Negative, not yet done:** doc comments and prose outside `ResourceCommandHandler.cs` itself still describe
  the retired `Outcome<TSuccess, NotFound, Forbidden>` shape - `Interlocutor.Generator`'s `INTERLOC010`
  (`ValidationShapeMismatch`) diagnostic remarks, `TutorialProject`'s `CompleteTodoTaskHandler.cs`/
  `CompleteTodoTaskCommand.cs` contrastive comments, and any `documentation/` page naming the old signature. This
  is deliberately left as follow-on work (`.scratch/resource-command-handler-case-fidelity/issues/03-*`), not a
  silently forgotten gap - mirroring how ADR-0025 recorded its own remaining five behaviors as open follow-on
  rather than pretending the migration was total.

## Alternatives considered

- **Falling back to a plain rename, `TSuccess` -> `TResponse`, keeping the fixed `Outcome<TResponse, NotFound,
  Forbidden>` shape.** This was the PRD's named fallback if the spike found `Eject.WithCase` didn't reach this
  consumer without disproportionate change. Rejected once the spike proved otherwise: the mechanism reaches
  `ResourceCommandHandler` through the existing wrapper/executor catch with no new catch point, no per-consumer
  `IOutcomeCaseRegistry` dependency, and no change to the dispatch machinery - the harder redesign was both
  feasible and cheaper to reach than the spike's framing worried it might be, so there was no remaining reason to
  settle for the narrower rename.
- **A `ResourceCommandHandler`-specific catch point**, e.g. wrapping the base class's own `HandleAsync` body in a
  `try`/`catch (PipelineEjectCaseException)` and reconstructing `TResponse` itself. Rejected - this would have
  reintroduced exactly the per-consumer `IOutcomeCaseRegistry` dependency `Eject.WithCase` was built to avoid
  ([ADR-0025](0025-eject-withcase-type-erased-variant.md)'s whole premise), and the spike found it was
  unnecessary: the wrapper-side catch already sits around this class's `HandleAsync` as the chain's innermost
  delegate, with nothing left for a second catch point to do.

## Follow-on work

- Update `Interlocutor.Generator`'s `INTERLOC010` (`ValidationShapeMismatch`) diagnostic remarks, `documentation/`
  pages, and `TutorialProject`'s `CompleteTodoTaskHandler.cs`/`CompleteTodoTaskCommand.cs` comments that still
  name `ResourceCommandHandler<TCommand, TEntity, TSuccess>` or its old fixed `Outcome<TSuccess, NotFound,
  Forbidden>` shape (`.scratch/resource-command-handler-case-fidelity/issues/03`).
