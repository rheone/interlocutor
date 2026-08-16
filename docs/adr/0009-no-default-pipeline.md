# ADR-0009: No Default Pipeline - Flat Ordered Filtered Behavior List

**Status:** Accepted

## Context

A bare `AddInterlocutor()` call today registers logging, auditing, and authorization behaviors whether or not the consuming application uses any of them - including logging on requests that have nothing worth logging. A consumer who wants only the mediator and their handlers cannot get that; they get five pipeline behaviors and three service registrations they never asked for, and the only way to see what actually runs on a given request is to read the library's registration code, not their own.

Separately, ordering today is implicit in `AddInterlocutor()`'s internal registration sequence. A consumer composing their own subset of behaviors has no documented model for where a new behavior lands relative to the built-in ones.

## Decision

The pipeline is **empty by default**. A bare registration call produces the mediator, discovered handlers, and discovered stages - zero pipeline behaviors. Nothing cross-cutting runs unless the consumer's own builder calls add it.

- **Flat ordered list, builder-call order.** Behaviors form one flat list in the order the builder registered them - no named profiles, no numeric priority values, no merge rules between "default" and "custom" behaviors, because there is no default list to merge into. Reading the builder configuration top to bottom tells you the execution order; there is no second source of truth to reconcile it against.
- **Filtered by marker-interface constraint, order preserved.** A given request's chain is that same flat list filtered down to the entries whose generic constraint the request satisfies (the marker interfaces this library already ships - `ITransactionalRequest`, `IAuditableRequest`, `IRequiresPolicy`, etc.), with relative order preserved. A behavior scoped to `ITransactionalRequest` requests only executes, in its registered position, for requests implementing that interface.
- **Duplicates are legal.** Registering the same behavior type more than once is legal and runs it more than once, at both registered positions. No de-duplication, no warning - composition is not artificially restricted.
- **A behavior's own dependencies are try-add registered only when that behavior is registered.** A consumer who never adds `AuthorizationBehavior` never carries an `IPolicyEvaluator` registration they don't use.
- **The recommended chain is opt-in sugar, never a default.** Logging, then auditing, then authorization, then validation, then transaction remains the library's documented recommendation, available as a single builder call that adds exactly that list in that order - but it is sugar over the same builder API a consumer would use to compose their own chain, not special-cased default behavior. The rationale for that order (auditing outside authorization so a denied request is still recorded; transaction innermost so only handler work is transactional) is documented alongside it, because a developer composing their own chain needs the reasoning, not just the sequence.

## Consequences

- **Positive:** A bare registration call has zero surprise behavior. What runs on a request is fully readable from the builder configuration.
- **Positive:** No profile system, no ordering-value scale to reason about, no merge semantics between "the library's defaults" and "my additions" - there is only one list.
- **Positive:** Scoping via the existing marker interfaces means "which requests does this behavior touch" has one mechanism, already documented, rather than a second configuration surface.
- **Negative:** A consumer who wants the previous all-on behavior must explicitly opt into the recommended chain (or their own composition) - this is a breaking change from the current `AddInterlocutor()` defaults. Acceptable pre-1.0, with no migration shim shipped (see [ADR-0008](0008-compile-time-handler-discovery.md) and the PRD's breaking-release note).
- **Negative:** Registering a behavior twice with no warning can be a genuine mistake as easily as a deliberate composition. Accepted - the alternative (silent de-duplication or a warning heuristic) would need to guess intent the library cannot know.

## Alternatives considered

- **Named pipeline profiles (e.g. "Standard", "Minimal"):** Rejected - a profile is a default in a costume; it still hides "what runs" behind a name a developer has to go look up, and profiles inevitably drift as needs diverge from the presets.
- **Numeric ordering values on each behavior registration:** Rejected - a global integer scale invites collisions and mid-range insertions as behaviors accrete, and doesn't communicate intent the way call order already does for free.
- **Keep the current implicit defaults, add an opt-out:** Rejected - "runs unless disabled" is still ambient behavior a developer can miss; "runs only if added" is the only model where reading the configuration is sufficient.
