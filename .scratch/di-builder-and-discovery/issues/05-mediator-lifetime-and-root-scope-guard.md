## Labels

ready-for-agent

## What to build

Make the most damaging misuse of the library loud instead of silent.

Today a developer can inject the mediator into a singleton - a background service, a hosted service, a cache. In development the container's scope validation may catch it. In production, where that validation is off by default, it silently resolves request-scoped services from the root provider, so a scoped database context becomes process-lifetime and is shared across threads for the life of the application. The Autofac path leaks similarly: instances resolved from the root are rooted there and never disposed.

Register the three mediator interfaces as scoped, resolving a single instance, with the lifetime **not configurable**. Scoped rather than transient is the point: transient-into-singleton is legal in every container and nothing would flag it, whereas scoped lets the container's own validation catch the mistake at startup. A lifetime knob whose only wrong setting is silent data corruption is not a feature.

Then add the guard for what validation cannot catch - Autofac, and the Microsoft container in production. A root-scope detector abstraction lives in the shared registration core; each container package implements it in the way that container allows. Dispatch attempted from the root provider throws a named exception identifying the request type and telling the developer the fix.

The mediator must continue to borrow the ambient scope rather than create its own. That is what makes the unit-of-work marker, transient services, and disposables behave correctly, and creating a scope per dispatch would break sharing with the caller. A handler that genuinely needs an independent scope injects the standard scope factory; no service-locator API is exposed to handlers.

## Acceptance criteria

- [ ] All three mediator interfaces register scoped and resolve one instance
- [ ] No configuration option exposes the mediator's lifetime
- [ ] A root-scope detector abstraction lives in the shared registration core with a per-container implementation
- [ ] Dispatching from the root provider throws a named exception naming the request type and the remedy
- [ ] Integration test: a singleton consuming the mediator fails loudly in both containers
- [ ] Integration test: dispatch from a proper scope shares one scoped dependency instance across handler and pipeline
- [ ] The mediator does not create its own scope

## Blocked by

03 - Builder API, alongside the legacy overloads
