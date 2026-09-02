Status: needs-triage
Severity: HIGH
Category: architecture
Source: architecture.md lane, confirmed by antagonist lane with a documented-intent caveat

# `ResourceCommandHandler.Authorize` can never surface `Forbidden` — only generic `Failure`

## Summary

`ResourceCommandHandler<TCommand, TEntity, TSuccess>` — the library's own built-in resource-authorization template — is hard-typed to `Outcome<TSuccess, NotFound, Failure>`, and its `Authorize` abstract method returns `Failure?`. A denied resource-authorization check can therefore only ever surface as unclassified `Failure`, never as the purpose-built `Forbidden` case — even though `AuthorizationBehavior` (the library's *other* authorization mechanism, documented side-by-side with this one) correctly uses `Forbidden` for the equivalent denial.

This bakes the exact anti-pattern CLAUDE.md's result-type convention warns against ("don't route every handler failure through a single generic `Failure`") into the library's own shipped API, with no way for a consumer to opt out — the base class's `Handle` signature fixes the shape.

## Caveat

This is *documented, intentional* behavior, not a silent gap: `documentation/handler-selection-and-dispatch.md:189` states explicitly that `ResourceCommandHandler` always returns `Outcome<TSuccess, NotFound, Failure>` and "the shape is fixed and enforced by the handler's own contract." So the inconsistency is with CLAUDE.md's cross-cutting house convention and with `AuthorizationBehavior`'s own precedent, not a doc/code mismatch — still a real API smell worth fixing.

## Suggested action

Widen the fixed shape to `Outcome<TSuccess, NotFound, Forbidden>` (or `Outcome<TSuccess, NotFound, Forbidden, Failure>` if a genuinely-unclassified failure case is still needed for other operational errors), and change `Authorize` to return `Forbidden?`. This is a breaking API change — route it through the same deprecation/versioning path other `Outcome`-arity changes in this repo have used (see `.scratch/outcome-naming-and-arity-refactor/`).

Note: when adding regression tests for [01-outcome-null-payload-no-discriminator.md](01-outcome-null-payload-no-discriminator.md), include a case routed through `ResourceCommandHandler` — a consumer picking a nullable `TSuccess` here hits that CRITICAL bug through this same fixed-shape API.
