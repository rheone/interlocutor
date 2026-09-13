# Pipeline Operational Diagnostics & Config-Bound Behaviors

Status: not started. Tracked as issues under `.scratch/pipeline-ops-and-config/issues/`.

**Scope note:** the registration-health-check capability originally scoped here was withdrawn (the maintainer reneged on it) - it is not part of this PRD. Two capabilities remain.

## Problem

Every lever `InterlocutorBuilder`/`InterlocutorPipelineBuilder` expose today is a compile-time, startup-time decision: which behaviors run, which implementation a dependency (`ITimeoutPolicy`, `IRateLimiter`, `IAuditSink`, ...) resolves to, and whether the accumulated registrations are internally consistent (`ValidateOnBuild`). That's deliberate and mostly right (see ADR-0009, ADR-0014, ADR-0019) - but it leaves two real operational gaps once an application built this way is actually running:

1. **Nothing answers "what will actually run" against a live container.** `InterlocutorBuilder.DescribeRegistrations()` reports what the builder was *told* to do, computed before a container exists. `PipelineChainResolver.ResolveChain<TRequest,TResponse>` (the thing that actually decides, per ADR-0019's two-tier model) is public but generic-only and unpackaged as a diagnostic - a support engineer with a request type name from a ticket has no first-class way to ask "what ran for this."
2. **Every pipeline dependency is compile-time-swappable only.** Turning a new, riskier behavior on for a subset of traffic, or adjusting a numeric threshold, both require a code change and a redeploy - there's no config-bound lever anywhere in the pipeline surface today.

## Decision

Two additive capabilities, both optional, neither changing behavior for a consumer who doesn't opt in:

1. **Runtime pipeline explainer** (issue 01) - a small diagnostic service that answers, against a live `IServiceProvider`, which behaviors resolve for a given request/response type pair, including which Tier-1 override rule (if any) matched.
2. **Conditional ("canary") behavior wrapper** (issue 02) - a way to gate an existing `IPipelineBehavior<,>` behind a runtime predicate, with a shipped percentage-rollout gate, so a new or risky behavior can be turned on for a fraction of traffic and ramped without a redeploy.

### The configuration-pattern split

All three need a runtime-adjustable setting (enabled/disabled, a tolerance threshold, a rollout percentage). The container-neutral core (`Interlocutor.DependencyInjection.Abstractions`) stays configuration-mechanism-agnostic per its existing design - it takes a plain POCO settings object or an `Action<TOptions>` delegate on the builder, the same shape every other builder method already uses. `Microsoft.Extensions.Options` binding (`IOptions<T>`/`IOptionsMonitor<T>` from `IConfiguration`) is additive sugar that only `Interlocutor.DependencyInjection.Microsoft` provides, by preferring an already-registered `IOptions<T>`/`IOptionsMonitor<T>` for the settings type over the builder-supplied value when one is present - the same "consumer's own registration wins" precedent `Defaults` already establishes. Autofac gets the identical POCO-based configuration without any `IOptions` dependency, since that pattern isn't native to it.

## Non-Goals

- **Per-call-site pipeline shaping** (e.g., `sender.SendAsync(request, opts => opts.Skip<TransactionBehavior>())`). This is in real tension with ADR-0019's "declared once, centrally, no scenario where call order changes which implementation is registered" design. `OverridePipelineFor`'s predicate (already scoping to N types) and the delegate-form `AddBehavior<TRequest,TResponse>(onEnter, onExit)` (already an ad-hoc, no-class-required behavior) already cover the "one-off pipeline" need without introducing call-site variability. Not in scope here; revisit only if a concrete driving scenario emerges that those two don't cover.
- **True shadow/observe-only execution** (run a behavior's logic but discard its effect on the outcome). Real value, but mechanically heavier than a simple on/off or percentage gate - a behavior that short-circuits or transforms the response has no generic "run it but ignore what it did" semantics. Issue 03 ships the simpler percentage/toggle gate only; shadow mode is a fast-follow if real usage demands it.

## Verification

- Full solution build and test run; no behavior change for a consumer who doesn't call any of the three new APIs.
- Each new capability has its own focused test coverage (see each issue's acceptance criteria).
- `documentation/pipeline-and-behaviors.md` and `documentation/builder-and-registration.md` gain coverage of all three.
