> **Status: done**

## Labels

ready-for-agent

## What to build

- Review pass over `documentation/sample-app-walkthrough.md`: verify against the actual `samples/` project, remove stale language, add glossary links where appropriate.
- Review pass over `documentation/beginners-guide-todo-api.md`: **stays exactly one file, never split.** Fix stale language and verify accuracy, but be conservative with glossary-linking here - it's a self-contained tutorial, so most terms are already explained in context and shouldn't get an additional glossary link too.
- Move `maintainer_docs/guides/composing-operationoutcome-arities.md` to `documentation/composing-operationoutcome-arities.md` (it's consumer-relevant guidance already cited from `documentation/messages-and-result-types.md` today - a house-style violation this move resolves). Fix the inbound link in `messages-and-result-types.md` and any other references. Add it to `documentation/index.md`'s table of contents. Remove its entry from `maintainer_docs/README.md`'s "Contributor guides" section.

## Acceptance criteria

- [x] `beginners-guide-todo-api.md` remains a single file
- [x] `composing-operationoutcome-arities.md` lives at `documentation/composing-operationoutcome-arities.md`, linked from `documentation/index.md`
- [x] No remaining reference to its old `maintainer_docs/guides/` location (except historical, already-resolved tickets under `.scratch/di-builder-and-discovery/`, left untouched as history)
- [x] No stale language remains in either tutorial file

## Findings beyond the original scope

- `beginners-guide-todo-api.md` and `sample-app-walkthrough.md` were both verified in depth (the former describes a hypothetical project, the latter a real, compiling sample under `samples/` that's part of the solution build) and found accurate - the guide's "caller-chosen outcome type" section for bare `ICommand` already correctly describes the exact nuance found wrong elsewhere in unit 04. Only one stale reference (`IOperationOutcomeMapper` → `IOutcomeStatusMapper`, from unit 05's rename) needed fixing.
- `composing-operationoutcome-arities.md` (the moved guide) needed substantial fixes before it was fit to publish as consumer-facing content:
  - Used a nonexistent `ValidationFailure` type nine times throughout - the real type is `ValidationError`. Renamed throughout.
  - Every example shadowed the `OperationOutcome` type name with a local variable of the same name (`var OperationOutcome = await sender.Send<OperationOutcome>(...)`) - confusing even where legal. Renamed to `result`.
  - Example 2's entire "why `AuthorizationBehavior` needs per-closed-pair registration" narrative described a design the source code's own XML docs say was replaced: `AuthorizationBehavior<TRequest, TResponse>` is a plain open-generic behavior today (constrained only to `TRequest : IRequiresPolicy`, `TResponse : IOperationOutcome`), registered once like every other behavior, not the old `AuthorizationBehavior<TRequest, TSuccess>` shape the guide described. Rewrote the composition root and the summary table's claims about which arities it supports.
  - Example 3 justified its inline ownership check with the same wrong "arity mismatch" reasoning; the real reason is that ownership is resource-dependent authorization (needs the loaded entity), which `AuthorizationBehavior` can never do regardless of arity - fixed to match `handler-selection-and-dispatch.md`'s existing Resource vs. Non-Resource distinction, with a cross-link.
