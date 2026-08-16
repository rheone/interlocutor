# PRD: DI Builder, Compile-Time Discovery, and Pipeline Composition

Status: ready-for-agent

## Problem Statement

Interlocutor promises compiler-checked correctness - handlers return outcomes as unions so the compiler forces callers to handle every case - but the moment a developer wires the library into an application, that promise stops applying. Registration is runtime reflection with silent failure modes, and the behavior a developer gets depends on things they cannot see in their own code.

Concretely, a developer using Interlocutor today hits these:

- They set a configuration option and nothing happens. Three properties on the options object are never read: the default publish strategy, the outcome describer type, and startup validation. Publishing always uses the sequential strategy regardless of configuration.
- They register their own audit sink, policy evaluator, or request-context accessor, and whether theirs or the library's wins depends on whether their call sits before or after the library's registration call. Nothing warns them.
- They add the library and silently inherit five pipeline behaviors and three service registrations they never asked for, including logging on requests that have nothing worth logging.
- They write a handler, forget to register the assembly, and find out at runtime on the first request rather than at build time.
- They inject the mediator into a background service or hosted service. In development this may throw; in production, where scope validation is off by default, it silently resolves a scoped database context from the root provider and shares it across threads for the life of the process.
- They want to dispatch a message deserialized off a queue for an outbox or inbox, and cannot - dispatch requires the static request type, so they must write their own reflection.
- They read the domain glossary and find types that do not exist, HTTP status codes baked into a library that should not know about HTTP, and documentation that disagrees with the code about which status a validation failure maps to.
- They switch containers and find that the Microsoft and Autofac packages carry independently maintained copies of the same scanning logic and their own separate options types.

The through-line: the library's registration and dispatch layer behaves like the thing the library was built to replace - implicit, reflective, and surprising.

## Solution

One builder, compile-time discovery, and nothing running that the developer did not ask for.

A developer configures Interlocutor through a single fluent builder that works identically across both supported containers. Handlers are discovered at **compile time** by a source generator that reports missing handlers and ambiguous matches as build diagnostics, so registration mistakes become build failures instead of runtime exceptions. The pipeline is **empty by default** - behaviors and stages run only when explicitly added, in the order they were added, scoped to the requests they apply to via generic constraints on the marker interfaces the library already ships.

Ambient behavior that cannot be seen from the call site is removed. Handler selection never depends on namespace or assembly location. Override is explicit - an attribute or a builder call, never file placement. Defaults are registered such that a consumer's own registration wins regardless of call order, and every default is individually replaceable.

The result types stay pure: no HTTP knowledge in the core, with status mapping confined to the ASP.NET Core package and pluggable there. And because discovery moves to compile time, the library becomes trim- and AOT-clean, which the incumbent alternatives are not.

## User Stories

**Registration and configuration**

1. As an application developer, I want a single builder entry point for configuring Interlocutor, so that I do not have to choose between several overloads that do subtly different things.
2. As an application developer, I want the same builder API whether I use Microsoft DI or Autofac, so that switching containers does not mean relearning configuration.
3. As an application developer, I want every configuration option I set to actually take effect, so that I can trust the API surface.
4. As an application developer, I want my own registration of an audit sink, policy evaluator, or request-context accessor to win over the library's default regardless of where I call it, so that override does not depend on statement order.
5. As an application developer, I want an explicit builder method to replace any individual library default, so that I can override after the fact when I cannot reorder my registrations.
6. As an application developer, I want a bare registration call to give me only the mediator, my handlers, and my stages, so that nothing runs that I did not ask for.
7. As an application developer, I want the library to register a behavior's dependencies only when that behavior is registered, so that I do not carry services I never use.
8. As an application developer, I want an opt-in helper that applies the recommended behavior chain in the documented order, so that I can get a sensible pipeline without composing it by hand.
9. As an application developer, I want the reasoning behind the recommended order documented, so that when I compose my own chain I understand what the ordering protects.
10. As a library maintainer, I want the container-neutral registration logic to live in one place, so that the Microsoft and Autofac packages cannot drift apart.

**Handlers and discovery**

11. As a handler author, I want my handlers discovered without registering each one, so that adding a handler is a one-file change.
12. As a handler author, I want a build diagnostic when a command or query has no handler, so that I find out at compile time rather than on the first request.
13. As a handler author, I want to write one generic handler that serves a family of generic requests, so that work done against an abstraction is written once.
14. As a handler author, I want a concrete handler to take precedence over a generic one that also matches, so that I can special-case a single request type without disabling the general handler.
15. As a handler author, I want two generic handlers that both match the same request to be a build error, so that dispatch is never silently arbitrary.
16. As a handler author, I want an explicit attribute or builder call to declare that one handler overrides another, so that override is visible in the code rather than implied by file location.
17. As a handler author, I want moving a file between namespaces or projects to never change which handler runs, so that refactoring is safe.
18. As a handler author, I want my handler's constructor dependencies injected by the container, so that handlers are ordinary services.
19. As an integrator, I want handlers in a referenced class library discovered without extra configuration, so that a multi-project solution works out of the box.
20. As an integrator working with plugins loaded at runtime, I want an explicit opt-in to runtime assembly scanning, so that reflection is available when I genuinely need it and never silently active when I do not.

**Pipeline behaviors and stages**

21. As an application developer, I want behaviors to run in the order I added them, so that I can read my configuration and know the execution order.
22. As an application developer, I want a behavior to apply only to requests carrying a given marker interface, so that auditing, authorization, and transactions target only the requests that opted in.
23. As an application developer, I want to register the same behavior more than once at different positions, so that composition is not artificially restricted.
24. As an application developer, I want to write a lightweight before/after hook without implementing a full behavior, so that simple cross-cutting work is cheap to add.
25. As an application developer, I want a before-hook to be able to decline a request and return an outcome without the handler running, so that short-circuiting does not require writing a behavior.
26. As an application developer, I want the after-hooks of every stage that already ran to still run when a stage short-circuits, so that paired setup and teardown are not skipped.
27. As an application developer, I want an exception in an after-hook logged rather than silently discarded, so that a failing hook is diagnosable.
28. As an application developer, I want to place the stage host anywhere in my behavior chain, including more than once, so that I decide what stages sit inside or outside - a transaction is only one possibility.
29. As an application developer, I want stage and behavior dependencies injected by the container, including for delegate-registered stages, so that hooks can use services.

**Notifications**

30. As a domain event author, I want a handler registered against a base event type or interface to receive every derived event, so that audit and outbox sinks can subscribe once.
31. As a domain event author, I want notification handler execution order documented as unspecified, so that I do not build on an accident.
32. As a domain event author, I want to declare that one handler runs after another by naming it, so that the rare real ordering need is expressible and compiler-checked.
33. As a domain event author, I want an ordering cycle reported as a build error, so that an impossible configuration cannot ship.
34. As a domain event author, I want a catch-all handler subscribing to every notification to be legal but surfaced as a build message, so that it is a deliberate choice rather than an accident.
35. As a domain event author, I want documented failure semantics for each publish strategy, so that I know whether some handlers may not have run.

**Dispatch and lifetimes**

36. As an integrator building an outbox, inbox, or queue consumer, I want to dispatch a request whose type is known only at runtime, so that I do not have to write my own reflection layer.
37. As an integrator, I want that runtime dispatch to return a typed outcome I can inspect for success or failure without knowing the concrete result shape, so that generic infrastructure can log and retry meaningfully.
38. As an application developer, I want a request's response type constrained to an outcome at compile time, so that the library's central convention is enforced rather than merely documented.
39. As an application developer, I want my handler and my pipeline to share the caller's unit of work, so that a transaction, transient services, and disposables all behave as expected.
40. As an application developer, I want a loud, named error when I inject the mediator somewhere it cannot resolve request-scoped services, so that I never get a silently process-wide database context.
41. As an application developer, I want the error to tell me the fix, so that I can resolve it without reading the library's source.

**Operations and documentation**

42. As an operator, I want dispatch traced through the standard .NET activity mechanism, so that requests appear in my existing tracing tools with no adapter.
43. As an operator, I want tracing to cost nothing when no listener is attached, so that instrumentation is safe to leave in.
44. As an application developer, I want to publish my application ahead-of-time compiled without trim warnings, so that I can use the library in size- and startup-sensitive deployments.
45. As an application developer, I want a runnable sample application, so that I can see the intended composition rather than reconstruct it from prose.
46. As an application developer, I want the core library to have no opinion about HTTP status codes, so that outcomes describe the domain rather than a transport.
47. As a web developer, I want the ASP.NET Core package's outcome-to-status mapping to be replaceable, so that I can choose my own conventions without forking.
48. As a new contributor, I want the glossary to describe types that actually exist, so that documentation is trustworthy.
49. As a new contributor, I want the variance rules explained, so that I understand why polymorphism applies to requests but can never apply to responses.

## Implementation Decisions

**Module structure**

- A new container-neutral dependency-injection abstractions module owns the builder, a neutral registration record (service type, implementation type, lifetime, kind), try-add versus add semantics, override resolution, and a root-scope detector abstraction. It depends only on the mediator abstractions module.
- A new source generator module emits registration modules, closed handler wrappers, the runtime-typed dispatch switch, and diagnostics. It depends on neither of the above at runtime.
- The Microsoft DI and Autofac modules become thin translators from the neutral registration record to their container's primitives, plus a container-specific root-scope detector. Both existing options types and every existing registration overload are deleted; the library is pre-1.0 and ships no migration shims.
- A new sample application module demonstrates end-to-end composition.

**Dispatch**

- Contravariant fan-out applies to notification handlers only. Queries and commands remain single-handler.
- Open generic handlers are supported and closed by the generator against the concrete request types it discovers. Handler-serves-an-unrelated-base-class dispatch is **not** supported.
- Selection rule, stated once: a closed handler beats an open generic handler; two open generic handlers matching the same request is a build error. There is no ranking by inheritance distance and no consideration of namespace or assembly location.
- An overrides attribute, living in the mediator abstractions module so handler assemblies take no DI dependency, is the sole ambiguity escape hatch.
- A runtime-typed dispatch overload accepts an object and returns the library's base outcome interface. It is backed by a generated type switch into the strongly-typed path; a reflection implementation exists only under explicit runtime scanning.
- Query and command request interfaces gain a constraint requiring their response type to implement the base outcome interface. This mirrors the constraint the unit-command dispatch overload already carries, and is breaking for consumers returning bare DTOs.
- Streaming is not supported. Handlers return materialized results. Paged result types are illustrations in documentation, not shipped types - a consumer needing paging defines their own paged type as a union payload.

**Discovery**

- The generator is the primary discovery mechanism; trim and AOT compatibility are goals. Reflection scanning is trim-annotated, opt-in by explicit builder call, and never active by default.
- Each handler-defining assembly emits its own registration module identified by an assembly-level attribute; the application-level generator discovers those modules by attribute and aggregates them. This keeps rebuilds incremental and lets a class library ship pre-generated registrations.
- The generator emits **registrations and dispatch, never direct construction**. Handlers, behaviors, and stages remain container-activated so constructor injection, decoration, and lifetime rules are unaffected. What the generator removes is the reflection used to build the dispatch wrapper, not the resolution of the handler inside it.

**Pipeline**

- A bare registration call produces the mediator, handlers, and stages, and zero behaviors. A behavior's dependencies are try-add registered only when that behavior is registered.
- Behaviors form a flat ordered list in builder-call order. A given request's chain is that list filtered to the entries that apply, preserving list position. There are no named profiles, no numeric ordering values, and no merge rules to reason about.
- Scoping a behavior to a subset of requests is expressed as a generic constraint on the request type parameter, using the marker interfaces the library already ships. Both containers skip open generic registrations whose constraints do not hold, and the generator resolves this at compile time.
- Registering the same behavior type more than once is legal and runs it more than once. No de-duplication, no warning.
- The recommended chain - logging, then auditing, then authorization, then validation, then transaction - survives as opt-in builder sugar and as documented rationale. It is never a default.

**Stages**

- The stage interface is contravariant on request only. Its response type parameter cannot be contravariant because it appears in the before-hook's return.
- The before-hook returns a dedicated two-state result rather than a nullable response. This is required, not stylistic: outcomes are value-type unions, so a defaultable response cannot distinguish "did not short-circuit" from "short-circuited with the default value." The shape, which encodes the decision more precisely than prose:

  ```csharp
  public readonly struct StageResult<TResponse>
  {
      public bool ShortCircuited { get; }
      public TResponse Response { get; }   // meaningful only when ShortCircuited
  }
  // default(StageResult<T>) is Continue - consistent with the
  // library's existing "safe at default value" convention.
  ```

- On short-circuit: remaining before-hooks are skipped, the handler is skipped, and after-hooks run in reverse order for exactly those stages whose before-hook ran - including the stage that short-circuited. Behaviors outside the stage host observe an ordinary response.
- After-hooks do not run when the handler throws. An exception thrown by an after-hook is caught, logged at error level with the stage and request type, and execution continues; an opt-in switch escalates to rethrow.
- Both hooks are no-ops by default via an abstract base class, and stages may also be registered as delegates. Delegate registration includes an overload receiving the service provider so lambda-form stages are not second-class.
- The stage host is an ordinary entry in the behavior list. The developer positions it, at any depth, and may register it more than once. It is explicitly **not** pinned relative to the transaction behavior - a transaction is only one thing a developer might wrap.

**Notifications**

- Execution order is explicitly unspecified. The escape hatch is relative, not numeric: attributes naming another handler to run after or before, topologically sorted, with cycles reported as a build error. Nested attributes are not legal C# - an attribute argument must be a constant, a type reference, or an array of those - so type references are the mechanism.
- Sequential publishing stops at the first exception and rethrows it unwrapped. Parallel publishing runs every handler and aggregates. Documentation states plainly that sequential leaves partial execution reachable.
- A catch-all handler subscribing to the base notification interface is legal and produces an informational build diagnostic.
- The publisher in the core is timing-free and immediate, running handlers in the caller's scope. The core knows nothing about commits, because a commit is only one possibility among many units of work. Deferral until after a commit, if ever wanted, belongs to the store-specific modules where the transaction behavior owns that concept.

**Lifetimes**

- The three mediator interfaces are registered scoped, resolving a single instance, and the lifetime is not configurable. Scoped rather than transient specifically so that the container's own scope validation catches a singleton capturing the mediator at startup; transient into singleton is legal in every container and would silently capture the root provider, producing a process-lifetime database context in production where scope validation defaults to off.
- The mediator never creates its own scope. It borrows the ambient one, which is what makes the unit-of-work marker, transient services, and disposables behave correctly. A handler needing an independent scope injects the standard scope factory.
- No service-locator API is exposed to handlers. The mediator's service provider is an implementation detail.
- A root-scope detector, implemented per container, throws a named exception identifying the request type and the fix when dispatch is attempted from the root provider.

**HTTP and result types**

- The core has no HTTP knowledge - no result type, XML documentation comment, or glossary entry references a status code.
- The ASP.NET Core module keeps its conversion helpers but delegates to a pluggable mapper with the current behavior as an overridable default, so mapping conventions are a consumer decision.

**ADR reconciliation**

- ADR-0002 (behavior onion model) is **amended**, not superseded. Its rejection of pre/post processors as a replacement for behaviors stands. The stage abstraction is an ergonomic layer that composes *into* the behavior chain through a single host behavior - it never forms a second pipeline. The amendment records the short-circuit contract.
- ADR-0003 (mediator as service locator) is **superseded** by a new discovery ADR. Its own listed alternative - source-generated registration - is now being taken, for diagnostics and AOT rather than for the startup-cost reason it anticipated. The new ADR must distinguish "the container is the handler registry" (still true) from "handlers may locate services themselves" (never true).
- ADR-0007 (publish strategies) is **amended**: its claim that handlers execute in registration order is replaced by explicitly unspecified ordering plus the relative-ordering attributes.
- ADR-0002 and ADR-0005 refer to result type names that no longer exist; update terminology while amending.
- New ADRs: compile-time discovery with per-assembly modules and AOT as a goal; no default pipeline with a flat ordered filtered behavior list; dispatch scope covering fan-out, open generics, and ambiguity as a build error; mediator scoped and non-configurable with a root-scope guard; HTTP-agnostic core with pluggable mapping; responses constrained to the outcome interface and streaming excluded.

## Testing Decisions

**What makes a good test here.** Tests assert observable behavior through the public composition surface: configure a builder, build a container, dispatch, and observe what ran, in what order, with what result. They do not assert how registration is stored, how wrappers are cached, or which internal type performed a step. A test that must change because an internal class was renamed is testing the wrong thing.

**Seams, in priority order.**

1. **Builder to container to dispatch** - the primary seam, carrying most coverage. Registration semantics, override precedence regardless of call order, behavior order and constraint-based filtering, duplicate behaviors running more than once, stage short-circuit and unwind order, constructor injection, and the root-scope guard are all observable here. Critically, one shared test body runs against both containers through a container-factory theory, so cross-container parity is structural rather than a second suite that can drift. Prior art: the existing Microsoft DI and Autofac dependency-injection test projects.
2. **Compilation** - generator diagnostics and emitted code can only be observed by compiling source, which is a different input medium and cannot fold into seam 1. A new generator test project using the Roslyn source-generator testing package asserts emitted-code snapshots and asserts every diagnostic's identifier, severity, and location. Coverage includes: no handler, ambiguous open generic match, closed-beats-open selection, missing override target, ordering cycle, catch-all notification handler, and cross-assembly module aggregation.
3. **Wrapper-level** - a deliberate exception to the "highest seam" rule, accepted because short-circuit unwind ordering has enough distinct paths that failure localization through seam 1 alone is poor. This seam is understood to be coupled to internals that this work actively reshapes, and is expected to churn; it must never be the only place a behavior is asserted.
4. **Architecture** - the existing dependency test project, extended with the new module boundaries: the DI abstractions module depends only on the mediator abstractions module; the generator module depends on neither; no module outside the ASP.NET Core one references ASP.NET Core. The commented-out validation-module assertion is resolved as part of this work.

**Explicitly not given a seam:** the neutral registration record, the root provider holder, and the outcome classifier. All are implementation details reachable through seam 1.

**Named scenarios worth calling out.**

- A handler, a behavior, and a stage each taking a scoped dependency all receive the same instance within one dispatch - asserted against both generated and reflection-discovered registrations, in both containers.
- A singleton consuming the mediator fails with the named root-scope exception in both containers.
- A consumer registering a default *before* the library call wins, and a consumer registering it *after* also wins via the explicit builder override.
- Publishing ahead-of-time compiles the sample with no trim warnings.

## Out of Scope

- **Streaming.** No streaming request abstraction and no async-sequence responses. Handlers return materialized results.
- **Paged result types.** Not shipped; documented as an example of a union payload a consumer defines themselves.
- **A rule-violation failure case.** Not shipped; it was only ever an example in the glossary.
- **Exception handler abstractions.** Directly contradicts the outcome-union thesis - expected failures are outcomes and exceptions signal defects. Adding a recovery hook would re-legitimize throwing for control flow.
- **Retry and resilience.** A consumer wraps their resilience library of choice as a behavior.
- **Caching behavior.** Invalidation is domain-specific; the documentation ships the pattern, not the implementation.
- **Any transport, message bus, or durable outbox.** In-process dispatch only. A durable outbox, if ever built, is a separate module.
- **Deferred-until-commit publishing in the core.** If built, it belongs to the store-specific modules.
- **Named pipeline profiles, numeric behavior ordering, and namespace or assembly proximity selection.** All considered and rejected.
- **A configurable mediator lifetime.** Its only incorrect setting causes silent data corruption.
- **Benchmarks.** Not selected for this scope.

## Further Notes

- **Variance is permanently request-side only.** The response type parameter cannot be made covariant or contravariant anywhere: it sits inside a task on handler interfaces, and in *both* an argument and the return position on the behavior interface. Documentation must state this plainly so nobody attempts it again. Handler interfaces are already contravariant on request today; what is missing is not the variance annotation but a resolution path that uses it.
- **This is a breaking release.** Every existing registration overload is deleted, both options types are deleted, the request interfaces gain a response constraint, and the stage interface changes shape. Pre-1.0, no shims.
- **The generator is the largest single cost.** It was chosen over a diagnostics-only analyzer after weighing them explicitly: the analyzer would deliver the build diagnostics at roughly a quarter of the cost, but only the generator delivers AOT compatibility and reflection-free runtime-typed dispatch, both of which are differentiators against the incumbent alternatives.
- **Two glossary terms need renaming.** "Void command" is a misnomer - those handlers always return an outcome; "unit command" is accurate. The failure type described in the glossary as a closed union of five cases is in fact a simple record struct carrying a reason and an error code, with the individual failure cases being independent types composed into an outcome union.
- **A documentation-only inconsistency to fix while nearby:** the glossary and the ASP.NET Core module disagree about which status a validation failure maps to. Removing HTTP from the glossary resolves it.
- Diagrams for the documentation: the behavior and stage composition with short-circuit unwind, and generator module aggregation across assemblies.
