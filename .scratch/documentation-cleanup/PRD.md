# Documentation Cleanup

Status: units 01-11 and 13-16 done; issues 18-19 done in ce515d2. Only unit 12 (full API reference) remains - scope/format decided 2026-08-30 (see its own file), but held `needs-triage` awaiting developer pickup rather than handed to an agent.

This effort is expected to recur as the codebase evolves (see `maintainer_docs/agents/documentation-maintenance.md`) - this file is the reusable record of scope, decisions, and unit breakdown for re-running it, not a one-shot ticket to close and forget.

## Original request (verbatim)

> Some of the user facing and developer facing documentation is out of date.
> Documentation speaks of "old" or "previous" states, it **MUST ALWAYS** speak of current state. This is not a released library, the only state is NOW (and potential future implementation that should not be user facing).
>
> Cleanup and improve existing documentation based on facts of the moment.
>
> Use git-hub style markdown and mermaid diagrams when appropriate.
> all documentation should be value added.
>
> This documentation need not be represented in the same manner and orginization as it is now, if it is appropriate to combine, split or reorganize documentation do so. The only document that should not be split or merged is the beginners guide.
>
> remove untruths as well as confusing and ambiguous documentation clearing it up by looking at actual implementation.
>
> documentation seems to be heavily focused on descriminated unions and http status codes. Descriminated Unions are a powerful option, and the package user should be aware if them, they aren't a major focus beyond where they are handy and flexible.
>
> mention anything that is obsolete as such (they may be going away, even before the first publish)
>
> break the cleanup in to managable meaningful contextual units
>
> create a full public API documentation broken down by package and namespace that can be used as the technical reference for this project.
>
> create documentation on registering "custom" types like interface makers and others, and how they can / should be configured so the packages are extensable

Follow-up clarifications from the grilling session that reshaped scope:

> claude / agents file should be updated to improve documentation similarly for each code iteration / feature implementation
>
> documentation dir, and readme.md in root is user/library user documentation; docs for are repository and code maintainer documentation. to clear this up rename 'docs' to 'maintainer_docs' and fix links as needed; all should be cleaned up or improved
>
> [full public API reference] hold this for now; create ticket but keep blocked untill told differently
>
> match existing convention; do not [imply the] meaning of yet to be implemented features or tickets. Items may be improved, but should not be destructive to preventing intended future changes
>
> improve inter document linking, especially to glossary when a word is first introduced in user facing documents - don't go over overboard especially if it is directly in context of the file that mentions it.

## Decisions standing for every run of this checklist

- **Scope**: `README.md` + `documentation/` (library-consumer docs) and `maintainer_docs/` (repo/code-maintainer docs, renamed from `docs/`) are both in scope.
- **`documentation/` speaks only of current state** - no "old/previous/legacy" language. `maintainer_docs/` may and should carry forward-looking content (ADRs' future-work notes, `TODO.md`, research docs) - improve it, never delete it just because it isn't "now."
- **ADRs are immutable point-in-time records** (this is `maintainer_docs/README.md`'s own rule) - link/typo fixes only, content is never rewritten, files are never merged or split.
- **The beginner's guide (`documentation/beginners-guide-todo-api.md`) stays one file**, always - review-and-improve only, never split.
- **Discriminated unions / `OperationOutcome` are one useful tool, not the headline** - mention where genuinely relevant, don't over-index.
- **HTTP status codes stay confined to `documentation/aspnetcore-integration.md`** - all other docs link there instead of restating status codes.
- **Obsolete members get an inline GitHub `> [!WARNING]` alert at first mention**, matching the alert style already used throughout `documentation/*.md`.
- **Glossary linking**: first mention of a `documentation/glossary.md` term in a user-facing doc links to it; skip repeat mentions in the same file and skip entirely when the file already explains the term in its own context.
- **Every markdown edit uses the `github-markdown` skill; diagrams use `mermaid-diagram-generator` only where genuinely clarifying**, never decoratively.

## Unit checklist

- [x] **01 - Infra**: `docs/` → `maintainer_docs/` rename + repo-wide reference fixes; delete stale `src/Interlocutor.EntityFrameworkCore`/`src/Interlocutor.NHibernate` leftover dirs; new CLAUDE.md "Documentation maintenance" section + glossary-linking House Style bullet + `maintainer_docs/agents/documentation-maintenance.md`; this backlog itself.
- [x] **02 - Core vocabulary**: `documentation/glossary.md`, `documentation/index.md`, `documentation/messages-and-result-types.md`.
- [x] **03 - Dispatch & registration**: `handler-selection-and-dispatch.md`, `builder-and-registration.md`, the four `dependency-injection-*.md` files (one of which, `dependency-injection-packages.md`, turned out redundant and was deleted).
- [x] **04 - Pipeline & lifecycle**: `pipeline-and-behaviors.md`, `mediator-lifetimes-and-scope.md`, `notifications-and-fan-out.md`, `runtime-typed-dispatch.md` (the last of these was rebuilt around a nonexistent `IOperationOutcome.IsSuccess`/`.Failure` API).
- [x] **05 - HTTP & persistence**: `aspnetcore-integration.md` (most-drifted file found so far - wrong mapper type, wrong status codes for `ValidationErrors`/`Failure`, missing `ToMinimalApiResult`), `transactions-and-store-integration.md`, `persistence-abstraction.md`, `persistence-implementations.md`.
- [x] **06 - Platform & quality**: `trim-and-aot-support.md` (accurate, no changes), `testing.md` (fixed a `.Value`-narrowing bug and a test contradicting the no-default-validation design).
- [x] **07 - Tutorials**: `sample-app-walkthrough.md`, `beginners-guide-todo-api.md` (both accurate as written), moved `composing-operationoutcome-arities.md` into `documentation/` after fixing a nonexistent type name and a description of `AuthorizationBehavior` matching a design the source says was already replaced.
- [x] **08 - Extensibility**: new `documentation/extensibility.md` - custom marker interfaces, custom `IPipelineBehavior<,>`/`IPipelineStage<,>`, custom `IOperationOutcome` cases, `AddTypeMatch`, `RequireUniqueClosure`/`Eject`, `OverridePipelineFor`, plus a table tracing where each hook is invoked.
- [x] **09 - `maintainer_docs/` top level**: `PRD.md` was the most drifted file in the whole sweep (bootstrap-era types, retired packages, reversed pipeline order, wrong dispatch mechanism) - rewritten end to end; `README.md`/`TODO.md` needed only small fixes.
- [x] **10 - `maintainer_docs/adr/*`**: verified all 19 files' links resolve programmatically - none were broken beyond the one ADR-0016 already fixed in unit 01. No content changes.
- [x] **11 - `maintainer_docs/agents/*`, `research/`, `bootstrapreference/`**: verified against every bug pattern found this sweep - none present. No changes needed.
- [ ] **12 - Full public API reference** (blocked - see `issues/12-full-api-reference.md`).
- [x] **13 - `DefaultPolicyEvaluator` allow-all warning**: added, plus fixed three instances of invalid `ICommand<Success>` found along the way (`Success` isn't `IOperationOutcome`).
- [x] **14 - `MediatorActivitySource`**: new "Distributed Tracing" section in `mediator-lifetimes-and-scope.md`.
- [x] **15 - `Delegate*` shortcut types**: added alongside each full-class example, plus the idiomatic `AddStage(onBefore, onAfter)` overload for stages.
- [x] **16 - Name the mediator's exception types**: named all three, and fixed a subtly wrong claim about when the root-scope guard actually fires.

## Known follow-up (found during unit 01, not part of the original scope)

`.claude/skills/setup-matt-pocock-skills/domain.md` and `.claude/skills/improve-codebase-architecture/SKILL.md` (generic, repo-independent skill files) hardcode `docs/adr/` as the default ADR location a domain-modeling skill expects. Since this repo's ADRs now live at `maintainer_docs/adr/`, `maintainer_docs/agents/domain.md` has been updated to say so explicitly - but if either of those skills is ever run against this repo without reading `domain.md` first, it will look in the wrong place. Not fixed here since those are shared skill files, not this repo's documentation.
