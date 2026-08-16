## Labels

ready-for-agent

## What to build

A `documentation/pipeline-and-behaviors.md` covering pipeline composition end to end, from the developer's perspective. Primary sources: ADR-0002 (onion/Russian-doll model, as amended for the stage short-circuit contract), ADR-0009 (no default pipeline), and `docs/PRD.md` §9 (pipeline behaviors table and designed order) — cite these rather than re-deriving the rationale from scratch, and verify every claim against the current `src/Interlocutor.Abstractions` source before writing it down.

Cover:

- **The no-default-pipeline model**: nothing runs unless asked for. Behaviors form a flat ordered list in call order; a given request's actual chain is that list filtered by generic constraints on the marker interfaces (`ITransactionalRequest`, `IAuditableRequest`, `IRequiresPolicy`, `IExternalStoreRequest` — link to `documentation/messages-and-result-types.md` from ticket 27 for the full marker-interface catalog rather than repeating it here). The same behavior type can appear more than once in the list.
- **The recommended chain and its rationale** — not as a default, clearly marked opt-in sugar. Explain *why* auditing sits outside authorization and why the transaction boundary sits innermost, not just the sequence itself.
- **Validation, non-resource authorization, and auditing behaviors**: what each one does, what marker interface opts a request in, and where each sits in the chain. (Resource-based authorization is *not* here — it's a template-method concern on the handler, covered in ticket 24 — link to it and explain briefly why it isn't a behavior: it needs the entity just loaded, and a behavior would need shared mutable state to pass that forward.)
- **Pipeline stages** (`IPipelineStage<TRequest, TResponse>`, `PipelineStageBehavior`): the lighter-weight `OnBeforeAsync`/`OnAfterAsync` hook model, `StageResult<TResponse>` short-circuiting, contravariant `TRequest`, and that a stage is registered as an ordinary entry in the same flat behavior list — never a second pipeline.
- **Short-circuit and reverse-unwind guarantees**: what happens to already-run behaviors' outbound/egress logic when something downstream short-circuits.

Include one inline `​```mermaid` diagram showing the behavior/stage chain with a short-circuit and the resulting reverse unwind — this is exactly the kind of composition a sentence can't make as clear as a picture. Don't add a diagram for anything prose already covers.

## Acceptance criteria

- [ ] No-default-pipeline model, flat ordered list, and generic-constraint filtering are explained with a concrete example
- [ ] Recommended chain appears with its rationale, explicitly marked as opt-in, not default
- [ ] Validation, non-resource authorization, and auditing behaviors are each covered, with a pointer to ticket 24's doc for resource-based authorization and why it's structurally different
- [ ] Pipeline stages, `StageResult` short-circuiting, and the "always both hooks invoked" contract are explained
- [ ] Short-circuit/reverse-unwind behavior is explained
- [ ] One inline mermaid diagram shows chain composition with short-circuit and unwind
- [ ] Every code example compiles against the shipped API
- [ ] Follows house conventions from ticket 21 (alerts, TBD markers, no-counts rule)

## Blocked by

21 — Establish documentation/ directory; retire CONTEXT.md
