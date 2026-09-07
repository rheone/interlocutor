# 03 - Update documentation for the renamed marker interfaces

**What to build:** Every doc that names one of the four renamed marker interfaces (`IRequiresPolicy` → `IRequiresPolicyRequest`, `ICancellable` → `ICancellableRequest`, `IHasTimeout` → `ITimeoutRequest`, `IRateLimited` → `IRateLimitedRequest`) is updated to the new name, so no stale identifier survives per the house truthfulness rule (every claim verified against `src/`, not carried forward from before the rename).

**Blocked by:** 01.

**Status:** done - implemented in `c2441b5`.

- [x] `CLAUDE.md`'s marker-interface list is updated to the new names.
- [x] `documentation/api-reference/interlocutor-abstractions.md`, `documentation/api-reference/interlocutor-dependencyinjection-abstractions.md`, `documentation/glossary.md`, `documentation/pipeline-and-behaviors.md`, `documentation/extensibility.md`, `documentation/best-practices.md`, `documentation/builder-and-registration.md`, `documentation/composing-outcome-arities.md`, `documentation/messages-and-result-types.md`, `documentation/handler-selection-and-dispatch.md`, `documentation/beginners-guide-todo-api.md`, and `documentation/sample-app-walkthrough.md` are updated wherever they name one of the four old markers. (`documentation/extensibility.md` had zero hits - correctly left untouched.)
- [x] `maintainer_docs/adr/0004-marker-interfaces-for-cross-cutting.md` reflects the new names (ADR text describes current reality; it is not amended to pretend the names were always this way - reword only the identifier, not the historical reasoning).
- [x] `maintainer_docs/PRD.md` and the `maintainer_docs/bootstrapreference/` materials (`Abstractions.cs`, `CompositionAndUsage.cs`, `ADR-mediator-result-pipeline.md`, `Addendum-B-library-packaging.md`, `Conversation.md`, `PRD-mediator-result-pipeline.md`) are updated wherever they name one of the four old markers.
- [x] No reference to the old four names remains anywhere under `documentation/` or `maintainer_docs/` (`rg` for `IRequiresPolicy\b|ICancellable\b|IHasTimeout\b|IRateLimited\b` returns nothing outside of prose explicitly discussing the rename's history, if any). Verified empty; also caught two ADRs beyond this ticket's own file list (`0009-no-default-pipeline.md`, `0014-di-builder-matching-registration-and-build-validation.md`) via the ground-truth grep.
