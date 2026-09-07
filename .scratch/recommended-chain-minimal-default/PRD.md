# Slim Down `UseRecommendedChain()` - Drop Auditing & Authorization

Status: not started. Tracked as issues under `.scratch/recommended-chain-minimal-default/issues/`.

## Problem / Decision

`InterlocutorPipelineBuilder.UseRecommendedChain()` currently composes five behaviors: Logging → Auditing → Authorization → Validation → Transaction (see `src/Interlocutor.DependencyInjection.Abstractions/InterlocutorPipelineBuilder.cs`, and the ordering rationale in README.md / `documentation/pipeline-and-behaviors.md`).

**Decision (directed by the maintainer): narrow `UseRecommendedChain()` to Logging → Validation → Transaction.** `AddAuditingBehavior()` and `AddAuthorizationBehavior()` remain exactly as they are today - fully supported, individually callable, with their existing default-dependency try-add behavior (`IAuditSink`/`LoggingAuditSink`, `IPolicyEvaluator`/`DefaultPolicyEvaluator`) unchanged. Only their *inclusion inside `UseRecommendedChain()`'s own method body* is removed.

**Rationale (confirmed by the maintainer):** the five-behavior chain does too much for a zero-config default. Most consumers won't want auditing at all - whether and where to audit is an application-specific decision, not something to opt into implicitly by calling a convenience method. And most consumers who *do* want authorization already have it earlier in the request path - ASP.NET Core's own authentication/authorization middleware, or an upstream policy engine, running before the mediator is ever invoked - making a second `AuthorizationBehavior` pass inside the mediator pipeline redundant, and in a mismatched-policy case, actively conflicting rather than merely superfluous. Logging, Validation, and Transaction don't have this problem: they're not typically duplicated by something already running earlier in the stack. `UseRecommendedChain()` should compose only the behaviors nearly every consumer wants, with Auditing and Authorization staying available exactly as before for the consumer whose application actually needs the mediator itself to own either concern.

This is a breaking change to `UseRecommendedChain()`'s documented, tested behavior. Any existing consumer relying on it for auditing/authorization for free needs a clear migration line: add `.AddAuditingBehavior().AddAuthorizationBehavior()` explicitly (in the existing recommended relative order - see the ordering rationale, unaffected by this change) to keep prior behavior. **No changelog entry** - the project has not yet had a first release, so no changelog exists yet; this change ships as part of normal pre-release development. (A changelog should be started at the first release, and this repo's pre-release history - including this change - is exactly the kind of thing a changelog would otherwise have captured.)

## What does *not* change

- `AddAuditingBehavior()`, `AddAuthorizationBehavior()`, their default-dependency try-adds, and their `Override*` counterparts (`OverrideAuditSink`, `OverridePolicyEvaluator`) - unchanged, still individually callable.
- The five-behavior ordering *rationale* (why Logging is outermost, why Auditing sits outside Authorization, why Authorization precedes Validation, etc.) - still true and still needs to be documented, just reframed as "if you add these behaviors, here's where they go relative to each other and to the new three-behavior default," not as the description of what `UseRecommendedChain()` itself does.
- `OverridePipelineFor` (Tier-1 per-request-type overrides) - entirely orthogonal, unaffected.

## Verification

- Full solution build and test run.
- Existing tests asserting `UseRecommendedChain()`'s resolved behavior list/count must be updated to the new three-behavior chain, not deleted without replacement - the five-behavior ordering must still have *some* test coverage, exercised via the individual `Add*Behavior` calls a consumer makes to reconstruct the old chain.
