> **Status: done** - implemented alongside the other pipeline-overrides tickets in ADR-0019.

## Labels

ready-for-agent

## What to build

Let a developer declare, for a specific request type or a predicate matching a set of request types, an independent, explicitly-ordered pipeline that replaces the general-purpose one entirely for matched requests - not modify it, not skip pieces of it, a full positive substitution. Registering such an override is the only action a developer takes; there is no complementary "skip" or "exclude" operation anywhere in the design, so a developer never has to reason about identifying and removing an existing registration - only about declaring what they want instead.

When a matching request dispatches through the library's ordinary typed `Send` API under Microsoft DI, it runs exactly the declared pipeline; every other request continues to draw from the shared general-purpose list exactly as today, with no behavior change for existing consumers who don't opt in.

A request that structurally satisfies one of the library's existing marker interfaces (auditable, requires-policy, etc.) but has also been given an override must not have that marker-constrained behavior leak into its resolved pipeline - the override is enforced once, centrally, at the point where the pipeline chain is built, not by asking every behavior to check whether it should yield to one.

Document the new capability with a worked example, and record the decision - the two-tier model (explicit overrides beat one general-purpose fallback) and why enforcement lives at chain-construction time rather than as a per-behavior check - as a new architecture decision record alongside the existing ones this extends.

## Acceptance criteria

- [ ] A developer can declare an override pipeline for one or more request types, matched either by an explicit type or by a predicate (namespace, assembly, or other type-level criteria)
- [ ] A matched request's resolved pipeline is exactly and only the declared list, in the declared order
- [ ] A matched request that also satisfies an existing marker interface (auditable, requires-policy, etc.) does not run that marker-constrained behavior
- [ ] An unmatched request's resolved pipeline is unaffected - identical to today's behavior
- [ ] The override applies correctly through the library's ordinary typed dispatch API under Microsoft DI
- [ ] A new architecture decision record documents the two-tier model and the chain-construction-time enforcement choice
- [ ] `documentation/pipeline-and-behaviors.md` gains a worked example of declaring and dispatching through an override pipeline

## Blocked by

None - can start immediately.
