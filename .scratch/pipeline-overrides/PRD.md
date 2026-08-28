# Per-Request-Type Pipeline Overrides

Status: done - all four tickets implemented (ADR-0019).

## Problem

Interlocutor's pipeline (`docs/adr/0009-no-default-pipeline.md`) is a single flat, ordered list of `IPipelineBehavior<,>` registrations, filtered per-request purely by generic-constraint satisfaction. "No default pipeline" doesn't mean there's a prescribed pipeline every consumer is expected to build on — it means the library forces nothing on anyone. What a consumer wires up via `AddBehavior`/`Add*Behavior`/`UseRecommendedChain()` is a **general-purpose, convenience baseline** that fits most handlers in a typical application — not a canonical default every request is implicitly measured against. There's no way today for a specific handler (or group of handlers) whose needs diverge from that baseline to run a genuinely different behavior set — only to add to, or coincidentally miss, entries from the shared flat list.

## Decision

Add a way to declare, per request type (or a predicate over request types — namespace, assembly, explicit list), an independent, explicitly-ordered pipeline that a matched request gets **instead of** the general-purpose baseline — a full, positively-declared replacement, never a modification of it, and never dependent on knowing what the baseline contains.

### Model: two tiers, conflicts are build errors

- **Tier 1 — explicit, positively-declared overrides.** Zero or more override rules, each a predicate over request types plus an explicit, independently-ordered list of behavior types. A request type matched by a Tier-1 rule gets *exactly, only* that declared list — a full replacement, never a merge with or modification of Tier 2. Registering a rule is the only action a consumer takes; there is deliberately no "skip"/"exclude"/"remove" primitive anywhere in this design. That matters beyond the library's two unconstrained shipped behaviors (Logging, Validation): a subtraction-based mechanism would eventually need to identify and negate an arbitrary *consumer-registered* behavior too — by type? by registration index? what if it's registered more than once? Opt-in declaration of a complete replacement list sidesteps that problem entirely; there is never anything to remove.
- **Tier 2 — the general-purpose (baseline) pipeline.** Whatever's already registered via today's `AddBehavior`/`Add*Behavior`/`UseRecommendedChain()` calls, applied to every request type no Tier-1 rule claims. This requires no new mechanism — it's already what the container naturally resolves, so existing consumer configuration is unaffected unless they opt into Tier 1.
- **Conflicts within Tier 1** (two rules matching the same request type) are a build-validation error, not a silent resolution — mirroring the library's existing "ambiguous handler match is a build error" precedent. No precedence-declaration escape hatch in this pass — scope it as a fast-follow only if real usage demands it.

### API surface

A new builder method mirroring the shape of the library's existing predicate-based type-matching rule mechanism:

```csharp
builder.Pipeline.OverridePipelineFor(
    predicate: t => t.Namespace == "MyApp.Archival",
    name: "archival-pipeline",   // required - used in validation conflict messages
    behaviors: [typeof(SpecialAuditBehavior<,>), typeof(SpecialTransactionBehavior<,>)]
);

// Convenience overload for the common single-type case:
builder.Pipeline.OverridePipelineFor<ArchiveOrder>("archive-order-pipeline",
    typeof(SpecialAuditBehavior<,>), typeof(SpecialTransactionBehavior<,>));
```

Each listed behavior type is also registered with the container exactly as an ordinary behavior registration would be, so it can be resolved at dispatch time.

### Enforcement: decided once, centrally, at chain-construction time

At container-build time, every override rule is resolved against the known set of discovered request types, producing a registry mapping request type → its explicit behavior-type list. The library's request-dispatch wrappers consult this registry before building a request's behavior chain; if the request type is present, they resolve *exactly* the declared list, in declared order, in place of the ordinary resolution. If no rule matches, dispatch falls through to today's exact resolution, unchanged — the general-purpose baseline, untouched.

This is a deliberate choice over the alternative of having each behavior check, at the top of its own `Handle`, whether it should yield to an override. A per-behavior check's guarantee only holds if every behavior in the general-purpose tier — including any consumer-authored one — remembers to perform it; a forgotten check produces a silent, hard-to-trace leak ("I declared an override but X still ran"). Deciding centrally, once, at the point the chain is built, makes the guarantee airtight regardless of what any individual behavior's code does, and mirrors how the library already resolves handler-selection ambiguity: a single, structurally-enforced decision point, not N participants each asked to self-exclude.

Both of the library's dispatch paths (the ordinary typed API, and the runtime-typed path used for scenarios like outbox replay) need the identical check — they already independently reproduce the same base chain-building logic today, so this is adding the same small branch to both existing implementations, not a new kind of divergence between them.

### Validation

A new build-validation check, alongside the library's existing opt-in checks: for each discovered request type, if more than one override rule matches it, record a problem naming the conflicting rules. Surfaces only when build validation is explicitly requested, consistent with every other check of its kind in the library.

## Alternatives Considered

All of the subtraction-shaped alternatives considered share one structural weakness this design avoids entirely: "turn off behavior X for request Y" only ever solves the shipped-behavior case cleanly, because it needs a way to *identify and negate* whatever's being skipped. Extend that to a consumer's own custom behaviors and you need a real removal primitive. Positive, explicit declaration of a complete replacement pipeline never needs one: there's nothing to remove, only something to register.

- **Targeted closed registration alone / marker-interface opt-out** — already possible today for most marker-constrained shipped behaviors (withhold the marker); the two unconstrained ones have no lever, and even where it works it's after-the-fact subtraction from an ambient list, not a declared replacement.
- **A data-driven exclusion registry** — same subtract-only shape and the same removal-primitive problem, plus a new runtime configuration surface the library's no-default-pipeline design specifically avoided introducing.
- **A keyed-DI "override host" behavior positioned mid-chain** — structurally can't exclude behaviors positioned before it (the pipeline is onion-nested; an outer behavior has already run by the time an inner host gets control). This design keeps the useful part — a segregated, independently-resolved behavior set — but decides *before* dispatch starts rather than mid-chain.
- **An attribute on the handler class, generator-driven** — only reaches the library's rarely-used runtime-typed dispatch path without breaking a deliberately-non-public base class's visibility; becomes a second source of truth outside the builder. Not needed once the builder can already target one or more types directly.
- **A per-behavior registry check** — still a "does this apply to me, should I skip?" check performed by each behavior, rather than a positively-declared replacement resolved once; see "Enforcement" above for why the centralized alternative was chosen instead.
- **Replacing the existing handler-selection-override attribute with DI convention** — considered and rejected as out of scope: handler dispatch is single-valued, and DI's only native answer to that (silent last-registration-wins) is exactly the failure mode the library's compile-time handler discovery was built to eliminate. Not analogous to the pipeline case, where multi-registration resolution already composes gracefully.

## Verification

- Full solution build and test run, confirming no regression to existing pipeline-ordering and behavior-subset-constraint tests — untargeted requests must be byte-for-byte unaffected.
- New coverage proving the marker-leak-prevention guarantee: a request matched by an override rule that also structurally satisfies an existing marker interface must not run the marker-constrained behavior.
- New coverage proving both dispatch paths resolve an overridden request identically.
- New coverage proving conflicting override rules are caught by build validation.
