> **Amendment** — this ticket instructed citing ADR-0008 and ADR-0010 directly in the shipped `documentation/handler-selection-and-dispatch.md` prose. That instruction is superseded: `documentation/` files must state design rationale as fact and must never cite or link to `docs/adr/`, `docs/PRD.md`, or other `docs/` paths — see the house convention added in ticket 36. The shipped file has been corrected by ticket 36. No action needed here; historical context only.

## Labels

ready-for-agent

## What to build

A `documentation/handler-selection-and-dispatch.md` covering how a request finds its handler. Primary sources: ADR-0008 (compile-time handler discovery via source generator — supersedes ADR-0003's service-locator approach, mention the supersession briefly so a reader who finds ADR-0003 isn't confused), ADR-0010 (dispatch scope, open generics, build-error ambiguity), and `docs/PRD.md` §8 and §11.1 (handler contracts, startup validation).

Cover:

- **Generic handlers serving a family** of request types, and the rule that a closed handler beats an open one when both could apply.
- **Ambiguity as a build error**, not a runtime surprise — when two handlers could both serve a request, compilation fails rather than dispatch guessing.
- **The override attribute** for explicitly picking a handler when ambiguity would otherwise be a build error.
- **The explicit guarantee** that namespace and assembly location never affect dispatch — handler selection is purely type-shape-driven.
- **Resource-based authorization** via `ResourceCommandHandler`'s template method (load → authorize → handle), and why this lives on the handler rather than as a pipeline behavior: it needs the entity that was just loaded, and a behavior would need shared mutable state to pass that forward — which the design deliberately avoids. Link back to ticket 23's pipeline doc for how this differs from non-resource (policy-based) authorization.
- **Startup validation**: missing handler registrations fail at startup, not on first request.

## Acceptance criteria

- [ ] Generic-handler-family selection and closed-beats-open rule are explained with an example
- [ ] Ambiguity-as-build-error is demonstrated, including what the override attribute does about it
- [ ] The namespace/assembly-independence guarantee is stated explicitly
- [ ] Resource-based authorization via `ResourceCommandHandler` is explained, with the rationale for why it isn't a pipeline behavior
- [ ] Startup validation behavior (fails fast on missing registration) is documented
- [ ] Every code example compiles against the shipped API
- [ ] Follows house conventions from ticket 21

## Blocked by

21 — Establish documentation/ directory; retire CONTEXT.md
