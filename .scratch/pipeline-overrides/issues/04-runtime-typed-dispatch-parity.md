> **Status: done** - implemented alongside the other pipeline-overrides tickets in ADR-0019.

## Labels

ready-for-agent

## What to build

A request dispatched through the library's runtime-typed dispatch path (used for scenarios like outbox replay, where the request's static type isn't known at the call site) resolves an overridden pipeline identically to the same request dispatched through the ordinary typed API - no silent divergence between the two dispatch mechanisms for a developer who uses both.

Document that override rules apply uniformly regardless of dispatch path.

## Acceptance criteria

- [ ] A request type with a declared override pipeline resolves that same pipeline when dispatched through the runtime-typed path
- [ ] The same request dispatched through the ordinary typed API and through the runtime-typed path produce identical resolved pipelines
- [ ] An unmatched request's runtime-typed dispatch is unaffected
- [ ] `documentation/runtime-typed-dispatch.md` notes that override rules apply uniformly across both dispatch paths

## Blocked by

01 - Core override mechanism (Microsoft DI, typed dispatch path)
