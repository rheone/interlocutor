# ADR-0019: Per-Request-Type Pipeline Overrides

**Status:** Accepted

## Context

[ADR-0009](0009-no-default-pipeline.md) established the pipeline as a single flat, ordered list of `IPipelineBehavior<,>` registrations, filtered per request purely by generic-constraint satisfaction (marker interfaces, per [ADR-0004](0004-marker-interfaces-for-cross-cutting.md)). "No default pipeline" means the library forces nothing on anyone - it does not mean the flat list a consumer builds via `AddBehavior`/`Add*Behavior`/`UseRecommendedChain()` is a canonical default every request is implicitly measured against. It is a general-purpose, convenience baseline that fits most handlers in a typical application.

That baseline has no lever for a request whose needs genuinely diverge from it. A consumer can add to the flat list, or - for the two unconstrained shipped behaviors, `LoggingBehavior`/`ValidationBehavior` - withhold a marker interface a constrained behavior checks, but there is no way to give one request type (or a related group of them) an independent, differently-ordered pipeline. The only two levers today are "participate in the shared list" and "don't," never "run something else entirely."

## Decision

Add a way to declare, per request type or a predicate over request types, an independent, explicitly-ordered pipeline that a matched request gets **instead of** the general-purpose baseline - a full, positively-declared replacement, never a modification of it.

### Two tiers, conflicts are build errors

- **Tier 1 - explicit, positively-declared overrides.** Zero or more override rules (`InterlocutorPipelineBuilder.OverridePipelineFor`), each a predicate over request types plus an explicit, independently-ordered list of behavior types. A request type matched by a Tier-1 rule gets *exactly, only* that declared list. Registering a rule is the only action a consumer takes - there is deliberately no "skip"/"exclude"/"remove" primitive. A subtraction-based mechanism would eventually need to identify and negate an arbitrary *consumer-registered* behavior, not just the library's own two unconstrained ones - by type? by registration index? what if it's registered more than once? Positive, explicit declaration of a complete replacement pipeline sidesteps that problem entirely: there is never anything to remove, only something to register.
- **Tier 2 - the general-purpose baseline.** Whatever is already registered via `AddBehavior`/`Add*Behavior`/`UseRecommendedChain()`, applied to every request type no Tier-1 rule claims. No new mechanism - existing consumer configuration is unaffected unless it opts into Tier 1.
- **Conflicts within Tier 1** (two rules matching the same request type) are a build-validation error (`RegistrationProblemKind.ConflictingPipelineOverride`), not a silent resolution - mirroring `RegistrationProblemKind.AmbiguousClosure`'s existing "ambiguous handler match is a build error" precedent. There is no precedence-declaration escape hatch in this pass; a developer resolves a conflict by narrowing one rule's match criteria, not by declaring which one wins.

### Enforcement: decided once, centrally, at chain-construction time

At dispatch time, before a request's behavior chain is built, both dispatch paths consult a single shared decision point (`PipelineChainResolver.ResolveChain`, in `Interlocutor.Abstractions.Pipeline`): if an override rule matches the request's runtime type, the chain is exactly the declared list, in declared order; otherwise it falls through to today's ordinary resolution, unchanged. `OverridePipelineFor` registers each declared behavior type with the container exactly as an ordinary `AddBehavior` call would, so it is resolvable the same way; the resolver then filters and reorders whatever the container resolved down to the declared set.

This is a deliberate choice over the alternative of having each behavior check, at the top of its own `Handle`, whether it should yield to an override. A per-behavior check's guarantee only holds if every behavior in the general-purpose tier - including any consumer-authored one - remembers to perform it; a forgotten check produces a silent, hard-to-trace leak. Deciding centrally, once, at the point the chain is built, makes the guarantee airtight regardless of what any individual behavior's code does, and mirrors how the library already resolves handler-selection ambiguity: a single, structurally-enforced decision point, not several participants each asked to self-exclude.

Both of the library's dispatch paths - the ordinary typed API (`QueryHandlerWrapper`/`CommandHandlerWrapper`/`VoidCommandHandlerWrapper`) and the runtime-typed path used for scenarios like outbox replay (both the source-generator's emitted dispatch switch and its reflection-fallback, which itself re-invokes the typed API) - call the identical `PipelineChainResolver.ResolveChain` method. They already independently reproduced the same base chain-building logic before this decision; this adds the same one call to each, not a new kind of divergence between them.

### An unconstrained override behavior can still reach the general-purpose baseline

Because a declared override behavior is registered exactly as an ordinary behavior would be, an override behavior with no generic constraint of its own resolves for *every* request type via the container's ordinary open-generic resolution - not only the request type(s) an override rule names. This is not a special case this feature introduces: it is the same "an unconstrained generic behavior applies broadly" invariant [ADR-0009](0009-no-default-pipeline.md) already established for `AddBehavior`. A consumer who wants an override-specific behavior to run only for its intended request type(s) scopes it with a marker interface or a `where TRequest : ...` constraint, the same way the library's own marker-constrained behaviors (`AuditingBehavior`, `AuthorizationBehavior`) already scope themselves.

### Validation

A new build-validation check, alongside the library's existing opt-in checks (`InterlocutorBuilder.ValidateOnBuild`): for each discovered request type (query, value-returning command, or void command), if more than one override rule matches it, record a `RegistrationProblemKind.ConflictingPipelineOverride` problem naming the conflicting rules. Surfaces only when build validation is explicitly requested, consistent with every other check of its kind.

## Consequences

- **Positive:** A handler whose needs genuinely diverge from the general-purpose baseline gets a real, independently-ordered pipeline, without forcing every other request through the same divergence.
- **Positive:** The marker-leak-prevention guarantee is structurally enforced at one call site per dispatch path, not dependent on every current and future behavior remembering a self-exclusion check.
- **Positive:** Zero behavior change for a consumer who never calls `OverridePipelineFor` - the general-purpose baseline is computed exactly as before.
- **Positive:** A conflicting pair of override rules is caught at build-validation time (opt-in), not discovered as a silent, order-dependent surprise in production.
- **Negative:** An override behavior with no generic constraint of its own silently participates in the general-purpose baseline for every other request type too - a consumer must remember to scope a genuinely override-specific behavior, the same discipline `AddBehavior` already required and easy to overlook the first time.
- **Negative:** Declaring two overlapping override rules is only caught when `ValidateOnBuild()` is opted into; absent that, the registry resolves the first matching rule in declared order rather than failing loudly at runtime.

## Alternatives considered

All of the subtraction-shaped alternatives considered share one structural weakness this design avoids entirely: "turn off behavior X for request Y" only ever solves the shipped-behavior case cleanly, because it needs a way to *identify and negate* whatever's being skipped. Extend that to a consumer's own custom behaviors and a real removal primitive becomes necessary. Positive, explicit declaration of a complete replacement pipeline never needs one - there is nothing to remove, only something to register.

- **Targeted closed registration alone / marker-interface opt-out:** already possible today for most marker-constrained shipped behaviors (withhold the marker); the two unconstrained ones have no lever, and even where it works it's after-the-fact subtraction from an ambient list, not a declared replacement.
- **A data-driven exclusion registry:** same subtract-only shape and the same removal-primitive problem, plus a new runtime configuration surface [ADR-0009](0009-no-default-pipeline.md)'s no-default-pipeline design specifically avoided introducing.
- **A keyed-DI "override host" behavior positioned mid-chain:** structurally can't exclude behaviors positioned before it - the pipeline is onion-nested, so an outer behavior has already run by the time an inner host gets control. This design keeps the useful part (a segregated, independently-resolved behavior set) but decides *before* dispatch starts rather than mid-chain.
- **An attribute on the handler class, generator-driven:** only reaches the library's rarely-used runtime-typed dispatch path without breaking a deliberately-non-public base class's visibility; becomes a second source of truth outside the builder. Not needed once the builder can already target one or more types directly.
- **A per-behavior registry check:** still a "does this apply to me, should I skip?" check performed by each behavior, rather than a positively-declared replacement resolved once - see "Enforcement," above, for why the centralized alternative was chosen instead.
- **Replacing the existing handler-selection-override attribute with DI convention:** considered and rejected as out of scope - handler dispatch is single-valued, and DI's only native answer to that (silent last-registration-wins) is exactly the failure mode the library's compile-time handler discovery was built to eliminate. Not analogous to the pipeline case, where multi-registration resolution already composes gracefully.
