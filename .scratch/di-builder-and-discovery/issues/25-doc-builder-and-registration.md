## Labels

ready-for-agent

## What to build

A `documentation/builder-and-registration.md` covering how Interlocutor gets wired into DI. Primary sources: ADR-0008 (compile-time discovery/generator), `docs/PRD.md` §11 (DI registration — both Microsoft.Extensions.DependencyInjection and Autofac support assembly-scanning `AddInterlocutor()`).

Cover:

- **The builder as the single configuration entry point.** Include a clear migration note: every older registration overload is gone — a developer coming from an earlier version, or from a MediatR-style API, needs to know this isn't backward compatible and what replaced it.
- **Assembly-scanning registration** for both supported containers (Microsoft DI, Autofac) — what gets discovered, and what doesn't (namespace/assembly location doesn't affect dispatch, per ticket 24, but scanning still needs to know which assemblies to look at).
- **Generator module aggregation across assemblies**: how compile-time discovery composes when handlers live in a different assembly than the composition root.

Include one inline `​```mermaid` diagram showing generator module aggregation across assemblies — this is a case where the aggregation flow is genuinely hard to follow from prose alone.

## Acceptance criteria

- [ ] The builder is documented as the single entry point, with an explicit migration note for removed overloads
- [ ] Assembly-scanning registration is documented for both Microsoft DI and Autofac
- [ ] Generator module aggregation across assemblies is explained
- [ ] One inline mermaid diagram shows cross-assembly aggregation
- [ ] Every code example compiles against the shipped API
- [ ] Follows house conventions from ticket 21

## Blocked by

21 — Establish documentation/ directory; retire CONTEXT.md
