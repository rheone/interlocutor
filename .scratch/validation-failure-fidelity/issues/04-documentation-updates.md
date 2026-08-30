# 04 - Documentation updates

Status: not started

## Labels

ready-for-agent

## What to build

The generic "how to implement a custom pipeline behavior and swap out a built-in one" mechanism already exists in `documentation/extensibility.md` and `documentation/pipeline-and-behaviors.md` (marker interfaces, `AddBehavior`, `OverridePipelineFor`, `IOutcomeCaseRegistry` as the case construction/extraction seam). This ticket extends that existing material with the pieces added by issues 01-03, rather than duplicating it:

- **`documentation/extensibility.md`** - update the `IOutcomeCaseRegistry` "Where each hook is invoked" table row (currently lists `AuthorizationBehavior`, `CancellationBehavior`, `TimeoutBehavior`, `RateLimitedBehavior`, `Interlocutor.FluentValidation.ValidationBehavior`) to include `FluentValidationErrorsBehavior`. Add a short worked example showing a handler opting into `FluentValidationErrors` for full fidelity instead of `ValidationErrors`, alongside the existing extension-point examples.
- **`documentation/pipeline-and-behaviors.md`** - update or add to any example referencing `ValidationBehavior`/`ValidationErrors` specifically, so a reader sees both the portable default and the full-fidelity opt-in.
- **`documentation/aspnetcore-integration.md`** - the one file permitted to reference HTTP status codes; note `IValidationFailureCase`'s 422 mapping and that any implementer of it (not just the built-in types) is recognized.
- **`documentation/glossary.md`** - add `IValidationFailureCase` if it warrants a glossary entry, and link it on first mention in whichever of the above files introduces it first (skip the link on later mentions in the same file, per house style).

## Acceptance criteria

- [ ] Every code example added or changed compiles against the shipped API (house style truthfulness rule) - verify against the actual types added in issues 01-03, not carried forward from this ticket's own prose
- [ ] No citations to `maintainer_docs/adr/0020-*` or any other `maintainer_docs/` path - state the rationale as fact in `documentation/`, per house style
- [ ] No enumerated counts in prose beyond the closed-set exception (e.g. naming the specific fields `ValidationFailure` carries is fine; don't count them)
- [ ] `IValidationFailureCase` glossary-linked on first mention if a new glossary entry is added
- [ ] `documentation/extensibility.md`'s `IOutcomeCaseRegistry` table and worked examples reflect the new behavior
- [ ] `documentation/aspnetcore-integration.md` reflects the new 422 mapping

## Blocked by

01, 02, 03 - documentation needs to describe the shipped API accurately, not anticipate it.
