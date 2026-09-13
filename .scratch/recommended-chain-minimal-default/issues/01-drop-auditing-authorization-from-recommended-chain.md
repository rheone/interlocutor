# 01 - Drop Auditing & Authorization from `UseRecommendedChain()`

**Status:** ready-for-agent

**What to build:** Change `InterlocutorPipelineBuilder.UseRecommendedChain()` (`src/Interlocutor.DependencyInjection.Abstractions/InterlocutorPipelineBuilder.cs`) from:

```csharp
public InterlocutorPipelineBuilder UseRecommendedChain() =>
    this.AddLoggingBehavior()
        .AddAuditingBehavior()
        .AddAuthorizationBehavior()
        .AddValidationBehavior()
        .AddUnitOfWorkBehavior();
```

to:

```csharp
public InterlocutorPipelineBuilder UseRecommendedChain() =>
    this.AddLoggingBehavior()
        .AddValidationBehavior()
        .AddUnitOfWorkBehavior();
```

`AddAuditingBehavior()`/`AddAuthorizationBehavior()` themselves are untouched - still present, still callable individually, still try-adding `IAuditSink`/`IPolicyEvaluator` defaults exactly as today.

**Document *when* to still reach for `AddAuthorizationBehavior()`, not just that it's no longer default.** The rationale for dropping it is that most consumers already have authorization handled earlier in the request path - ASP.NET Core's own authentication/authorization middleware, or an upstream policy engine, running before the mediator is ever invoked. That reasoning has an important edge: it doesn't hold for a host with no such upstream layer - a message-bus consumer, a background worker, a scheduled job, anything dispatching a request with nothing HTTP-shaped in front of it. `AddAuthorizationBehavior()`'s XML doc and its coverage in `documentation/pipeline-and-behaviors.md` should say this explicitly, not just "here's how to opt back in": reach for it when the mediator itself is the first place authorization can be enforced, or when defense-in-depth against a fine-grained, resource-level check upstream middleware can't see is wanted even alongside upstream authorization. Give `AddAuditingBehavior()` the equivalent one-line "why you might still want this" treatment too, even though its own rationale (audit destination is an application-specific decision) doesn't have the same non-HTTP-host edge case.

This is a full sweep - every place that currently describes `UseRecommendedChain()` as five behaviors needs to change to three, and every place that carries the five-behavior ordering rationale needs to be reframed as "if you opt into Auditing/Authorization, here's where they go" rather than "here's what the recommended chain does":

- `UseRecommendedChain()`'s own XML doc comment and `<remarks>` (currently documents "logging, then auditing, then authorization, then validation, then transaction" with the full bullet-point rationale) - keep the ordering rationale (it's still correct guidance for a consumer who adds these behaviors back), reframe its framing.
- `documentation/pipeline-and-behaviors.md` - locate and update its `UseRecommendedChain()` coverage and ordering-rationale section.
- README.md's pipeline-behavior-ordering-rationale section (CLAUDE.md calls this "load-bearing" - read it before editing, don't re-derive the rationale from scratch).
- `documentation/glossary.md` line ~122 ("five behaviors: Logging → Auditing → Authorization → Validation → Transaction").
- Any other `rg -n "UseRecommendedChain|five behaviors" documentation/ README.md` hit not listed above.

**Write a new ADR** (`0026` - confirmed free as of this writing; 25 ADRs exist, `0001`-`0025`) recording: what changed, the confirmed rationale from the PRD (the chain does too much for a zero-config default - most consumers don't want auditing at all, and most who want authorization already have it earlier in the stack via ASP.NET Core middleware or an upstream policy engine, making a second in-pipeline pass redundant or conflicting), and the migration line for existing consumers (`.AddAuditingBehavior().AddAuthorizationBehavior()` to restore prior behavior, same relative position).

**No changelog entry needed.** This repo has no changelog yet (confirmed - no `CHANGELOG.md`, no convention recorded anywhere in `CLAUDE.md`/`maintainer_docs/`) because it has not yet had a first release. Do not invent one for this change alone; the ADR is the durable record.

**Acceptance criteria:**
- [ ] `UseRecommendedChain()` composes exactly Logging → Validation → Transaction
- [ ] `AddAuditingBehavior()`/`AddAuthorizationBehavior()` and their default-dependency/override behavior are unchanged
- [ ] Every existing test asserting `UseRecommendedChain()`'s resolved behavior list/count is updated to the new three-behavior chain
- [ ] New or retained test coverage proves the *old* five-behavior chain is still fully reconstructable by explicitly chaining `.AddLoggingBehavior().AddAuditingBehavior().AddAuthorizationBehavior().AddValidationBehavior().AddUnitOfWorkBehavior()`
- [ ] `UseRecommendedChain()`'s XML doc, `documentation/pipeline-and-behaviors.md`, README.md's ordering-rationale section, and `documentation/glossary.md` all updated - no remaining reference to "five behaviors" as what `UseRecommendedChain()` produces
- [ ] `AddAuthorizationBehavior()`'s XML doc and its `pipeline-and-behaviors.md` coverage explain *when* to still use it despite the default change - specifically calling out hosts with no upstream authorization layer (message-bus consumers, background workers, scheduled jobs), and defense-in-depth/resource-level checks upstream middleware can't see
- [ ] `AddAuditingBehavior()`'s doc coverage gets the equivalent one-line "why you might still want this" treatment
- [ ] New ADR-0026 written, recording the confirmed rationale (including the non-HTTP-host edge case for authorization) and an explicit migration line for existing consumers

**Blocked by:** None - can start immediately.
