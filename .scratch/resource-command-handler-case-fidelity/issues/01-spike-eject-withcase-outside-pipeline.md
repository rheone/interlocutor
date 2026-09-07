# 01 - Spike: does Eject.WithCase reach a non-behavior template-method consumer?

**What to build:** Prototype `ResourceCommandHandler<TCommand, TEntity, TResponse>` generic over `TResponse : IUnion` instead of the current fixed `Outcome<TSuccess, NotFound, Forbidden>` shape, ejecting `NotFound` and `Forbidden` via the already-shipped `Eject.WithCase<TCase>` (see `AuthorizationBehavior.cs`'s remarks and `PipelineEjectCaseException` for the mechanism as it exists today) instead of constructing them directly.

The open question this spike exists to answer: `Eject.WithCase` today is caught by the mediator's own pipeline-dispatch wrapper (`QueryHandlerWrapper`/`CommandHandlerWrapper`/the generator's emitted equivalents), which is the one place that knows `TResponse` concretely for an `IPipelineBehavior<TRequest, TResponse>` in the chain. `ResourceCommandHandler` is not a pipeline behavior - it's a template-method base class a concrete handler inherits from and separately declares as `ICommandHandler<TCommand, TResponse>` (see `documentation/sample-app-walkthrough.md`'s note on why that explicit interface declaration is required for handler discovery). Determine whether an ejected exception thrown from inside `ResourceCommandHandler.HandleAsync` actually reaches that same wrapper-side catch site given that indirection, or whether it needs its own catch point (and if so, where one could live without reintroducing a per-consumer `IOutcomeCaseRegistry` dependency, which is exactly what `Eject.WithCase` was built to avoid).

This is a spike to size the blast radius and prove (or disprove) feasibility, not a design already accepted - mirrors `pipeline-case-fidelity/07`'s own framing for the same mechanism's original introduction.

**Blocked by:** None - can start immediately

**Status:** ready-for-agent

- [ ] A prototype `ResourceCommandHandler<TCommand, TEntity, TResponse>` exists on a branch, ejecting `NotFound`/`Forbidden` via `Eject.WithCase` instead of constructing them directly, with at least one `TutorialProject` consumer migrated as a worked example
- [ ] The prototype proves where the ejected exception is actually caught and `TResponse` constructed - either the existing wrapper-side catch already covers this path, or a specific new catch point is identified
- [ ] Existing `ResourceCommandHandler` tests pass unchanged in observed behavior (same short-circuit results for not-found/forbidden, same success path) against the migrated prototype
- [ ] A short write-up of the actual blast radius found - whether the existing mechanism reaches this consumer unmodified, what (if anything) needs to change in the dispatch/wrapper machinery to make it reach, and any surprises - to feed issue 02's ADR-or-reject call

## Comments
