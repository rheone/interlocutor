# ADR-0026: `UseRecommendedChain()` Narrowed to a Minimal Default

**Status:** Accepted

## Context

`InterlocutorPipelineBuilder.UseRecommendedChain()` composed five behaviors: Logging → Auditing →
Authorization → Validation → Transaction (`src/Interlocutor.DependencyInjection.Abstractions/InterlocutorPipelineBuilder.cs`),
with the ordering rationale documented in README.md and `documentation/pipeline-and-behaviors.md`. As the
only sugar method the library ships for pipeline composition, it doubled as the de facto "just get me a
reasonable pipeline" default for a new consumer following the Quick Start.

Two of those five behaviors don't earn a place in a zero-config default:

- **Auditing** is an application-specific decision - where an audit trail goes (a dedicated store, a
  compliance pipeline, a plain log) and whether one is wanted at all isn't something a consumer should
  get for free by calling a convenience method. Most consumers don't want one.
- **Authorization**, for most consumers who do want it, is already enforced earlier in the request path -
  ASP.NET Core's own authentication/authorization middleware, or an upstream policy engine, running before
  the mediator is ever invoked. A second `AuthorizationBehavior` pass inside the pipeline is redundant
  there, and in a mismatched-policy case, actively conflicting rather than merely superfluous.

Logging, Validation, and Transaction don't have this problem - none of them is typically duplicated by
something already running earlier in the stack, and there's no plausible "I don't want this at all"
consumer for any of the three.

## Decision

Narrow `UseRecommendedChain()` to compose exactly **Logging → Validation → Transaction**.
`AddAuditingBehavior()` and `AddAuthorizationBehavior()` are unchanged - fully supported, individually
callable, with their existing default-dependency try-add behavior (`IAuditSink`/`LoggingAuditSink`,
`IPolicyEvaluator`/`DefaultPolicyEvaluator`) untouched. Only their *inclusion inside
`UseRecommendedChain()`'s own method body* is removed.

The five-behavior ordering rationale itself is not revisited or weakened - Logging outermost, Auditing (if
added) outside Authorization, Authorization (if added) before Validation, Validation before the
transaction, transaction innermost - it's reframed from "what `UseRecommendedChain()` does" to "where
Auditing/Authorization go if you add them back," across `UseRecommendedChain()`'s own XML doc,
`documentation/pipeline-and-behaviors.md`, README.md's Quick Start, and `documentation/glossary.md`.

`AddAuthorizationBehavior()`'s own documentation (its XML doc and its `pipeline-and-behaviors.md` coverage)
now states explicitly when it's still worth adding back, not just that it's opt-in: reach for it when the
mediator itself is the first place authorization can be enforced - a message-bus consumer, a background
worker, a scheduled job, or any host with nothing HTTP-shaped in front of it, where there is no upstream
middleware or policy engine to have already done this - or when defense-in-depth is wanted via a
fine-grained, resource-level check upstream middleware can't see, even alongside upstream authorization.
`AddAuditingBehavior()`'s documentation gets the equivalent one-line treatment: add it when your
application wants an audit trail, since where one goes is an application-specific decision the library
doesn't presume.

## Migration

An existing consumer relying on `UseRecommendedChain()` for auditing/authorization for free restores prior
behavior by chaining both back on explicitly, in the same relative position:

```csharp
builder.Pipeline
    .UseRecommendedChain()      // Logging -> Validation -> Transaction
    .AddAuditingBehavior()
    .AddAuthorizationBehavior();
```

This does **not** reproduce the prior relative order (Auditing/Authorization would land innermost, after
Validation and the transaction, rather than between Logging and Validation) - a consumer who cares about
the documented ordering, not merely about which behaviors run, instead reconstructs the full chain by
calling every step individually, in the original order:

```csharp
builder.Pipeline
    .AddLoggingBehavior()
    .AddAuditingBehavior()
    .AddAuthorizationBehavior()
    .AddValidationBehavior()
    .AddUnitOfWorkBehavior();
```

No changelog entry accompanies this change - the project has not yet had a first release, so no changelog
exists yet; this ships as part of normal pre-release development, and this ADR is the durable record of it.

## Consequences

- **Positive:** a new consumer following the Quick Start no longer gets an audit trail or an in-pipeline
  authorization pass they didn't ask for and, for authorization specifically, may already have redundantly
  or conflictingly enforced upstream.
- **Positive:** `AddAuditingBehavior()`/`AddAuthorizationBehavior()` remain exactly as capable as before -
  nothing about their own behavior, defaults, or `Override*` counterparts (`OverrideAuditSink`,
  `OverridePolicyEvaluator`) changed. A consumer who wants either back pays only the cost of one extra
  method call.
- **Negative, accepted: this is a breaking change to `UseRecommendedChain()`'s documented, tested
  behavior.** Every existing test asserting its resolved behavior list/count needed updating to the new
  three-behavior chain (`tests/Interlocutor.DependencyInjection.Abstractions.Tests/InterlocutorPipelineBuilderTests.cs`,
  and the Microsoft/Autofac DI integration test projects), and every documentation reference describing it
  as five behaviors needed reframing. A consumer upgrading without reading release notes silently loses
  auditing/authorization from their pipeline rather than failing loudly - there is no way to make a
  behavior-list narrowing fail fast the way, say, a removed public member would.
- **Negative, accepted: the migration line does not preserve relative order by itself.** Simply appending
  `.AddAuditingBehavior().AddAuthorizationBehavior()` after `.UseRecommendedChain()` restores presence but
  not position (both land innermost instead of between Logging and Validation) - a consumer who cares about
  the documented order must reconstruct the chain from the individual `Add*Behavior` calls instead, as
  shown above. This is documented at every migration line rather than silently left as a trap.

## Alternatives Considered

- **Leave `UseRecommendedChain()` at five behaviors and add a separate, smaller sugar method (e.g.
  `UseMinimalChain()`) instead of changing the existing one.** Rejected - this keeps the misleading default
  in place under its existing name for every consumer who doesn't discover the new method, and grows the
  builder's public surface for a distinction ("recommended" vs. "minimal") that isn't a real design
  trade-off a consumer needs both options for side by side.
- **Drop only Auditing, keep Authorization in the default.** Rejected - the redundant-or-conflicting-upstream
  problem is specific to Authorization, but the "consumer might not want this at all" problem applies to
  both; keeping Authorization in the default would still saddle every ASP.NET Core consumer using upstream
  middleware with a second, potentially conflicting policy evaluation pass they didn't ask for.
