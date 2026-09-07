# PRD: Rename `IVoidCommandHandler` to `IBareCommandHandler`

Status: **ready-for-agent**. Tracked as issues under `.scratch/bare-command-rename/issues/`.

## Problem Statement

`maintainer_docs/adr/0005-void-command-pipeline-support.md`'s own amendment already renamed this concept from "void command" to "unit command" in `documentation/glossary.md` and `documentation/messages-and-result-types.md`, because "void" wrongly implies the handler returns nothing when it always returns an outcome - only the success payload is empty. That amendment explicitly left the *shipped type names* unchanged at the time, so the code still says `IVoidCommandHandler`/`VoidCommandHandlerWrapper`/`IVoidHandlerWrapper` while the docs say "unit command" - a live mismatch between prose and code for the same concept. Separately, "unit" itself turns out to be a poor final choice for the type name: this library already ships a prominent, unrelated `IUnitOfWork`, and a "unit command" is explicitly, routinely paired with `IUnitOfWork` in the same sentence describing how void/unit commands are used - a real, in-repo terminology collision, not a hypothetical one.

## Solution

Rename `IVoidCommandHandler<TCommand, TResult>` to `IBareCommandHandler<TCommand, TResult>`, and its supporting wrapper types (`VoidCommandHandlerWrapper` → `BareCommandHandlerWrapper`, `IVoidHandlerWrapper` → `IBareHandlerWrapper`) to match. "Bare" was chosen after checking for collisions against three other candidates: "Unit" (rejected - collides with `IUnitOfWork`), "Terminal" (rejected - `maintainer_docs/agents/triage-labels.md` already uses "terminal state" for a different concept, ADR/issue lifecycle status), and "Empty" (rejected - soft collision with the pervasive `TypeAnalysisResult.Empty`-style "zero items in a collection" idiom used throughout `src/`). "Bare" has zero collisions anywhere in the codebase. This is a pure identifier rename with no behavioral change - the non-generic `ICommand` request-side marker itself is unaffected (it isn't "Void" or "Unit" or "Bare" today, and doesn't need to be; the mismatch is isolated entirely to the handler-side type names).

## User Stories

1. As a developer reading `IVoidCommandHandler<TCommand, TResult>` next to `IUnitOfWork` in the same file or the same sentence of documentation, I want the two names to stop implying a relationship that doesn't exist, so I don't have to pause and disambiguate "unit" every time.
2. As a developer who has read `documentation/messages-and-result-types.md`'s "Unit Command" heading, I want the actual type name in `src/` to match the concept name I just learned, so I'm not left wondering whether "unit command" and `IVoidCommandHandler` are the same thing.
3. As a maintainer completing ADR-0005's own flagged, unfinished amendment, I want the rename to land as a clean, mechanical identifier change, so it doesn't reopen a naming debate that was already settled once in prose.
4. As a consumer of `Interlocutor.Abstractions` upgrading across this change, I want a single, mechanical find-and-replace to fix my code, so the migration cost is proportional to the fact that this is a pure rename.
5. As a maintainer of `TutorialProject`, I want its `ClearCompletedTodoTasksHandler` (and any other consumer of the renamed types) updated in the same change, so the shipped worked example never references a type name that no longer exists.
6. As a maintainer of `documentation/glossary.md`'s "Unit command" entry, I want it updated to reflect the final chosen name, so the glossary doesn't end up naming a third term ("unit") that matches neither the shipped type ("Bare") nor the original ("void").

## Implementation Decisions

- Rename, across the whole solution: `IVoidCommandHandler<TCommand, TResult>` → `IBareCommandHandler<TCommand, TResult>`; `VoidCommandHandlerWrapper<TCommand, TResult>` → `BareCommandHandlerWrapper<TCommand, TResult>`; `IVoidHandlerWrapper` → `IBareHandlerWrapper`. Any generator-internal enum member or well-known-type-name constant referring to "Void" for this handler kind (e.g. in `Interlocutor.Generator.Models.HandlerKind`/`RequestKind`, `Interlocutor.Generator.WellKnownTypeNames`, `Interlocutor.DependencyInjection.Abstractions.GeneratedHandlerKind`) is renamed to match.
- The non-generic `ICommand` request-side marker interface is **not renamed** - it was never called "Void" or "Unit" in its own type name, only in surrounding prose describing it; the mismatch this PRD fixes is isolated to the handler-side types.
- Confirmed no naming collision: no type, member, or prose use of "Bare" exists anywhere in the current codebase (`rg -i "\bbare\b"` across `src/`, `documentation/`, `maintainer_docs/`) prior to this change.
- `documentation/glossary.md`'s "Unit command" entry, `documentation/messages-and-result-types.md`'s "Unit Command" heading and prose, and `maintainer_docs/adr/0005-void-command-pipeline-support.md`'s amendment note are all updated to reflect "Bare" as the final, shipped name - completing rather than re-litigating that ADR's amendment.
- This is independent of `.scratch/ioutcome-to-iunion-retirement/` - `IBareCommandHandler<TCommand, TResult>`'s own `where TResult : IOutcome` constraint is swapped to `IUnion` only if that PRD lands; this PRD does not depend on it and can ship in either order.

## Testing Decisions

- This is a pure identifier rename with no behavioral change - the existing test suite is the regression signal. Every test currently referencing `IVoidCommandHandler`/`VoidCommandHandlerWrapper`/`IVoidHandlerWrapper` (`tests/Interlocutor.Abstractions.Tests`, `tests/Interlocutor.DependencyInjection.Abstractions.Tests`, `tests/Interlocutor.DependencyInjection.Autofac.Tests`, `tests/Interlocutor.DependencyInjection.Microsoft.Tests`, `tests/Interlocutor.Generator.Tests`) is updated to the new names and must continue passing unmodified in behavior.
- No new test scenarios are introduced by this PRD - renames don't need new coverage, they need the existing coverage to keep passing under the new names.
- `dotnet build`/`dotnet test` across the whole solution, and `dotnet csharpier .` producing no diff, confirm the rename is complete and consistent.

## Out of Scope

- Any change to `ICommand` (non-generic) itself, or to the semantics of when a caller picks `TResult` at the `Send<TResult>` call site - unchanged.
- Any change to what arity a "bare" command's `TResult` may be - a bare command already supports every arity today (it isn't tied to arity 0), and nothing here changes that.
- The `IOutcome`-to-`IUnion` constraint swap - tracked separately, this PRD's constraint follows whichever that PRD lands as, in either order.

## Further Notes

- "Unit," "Terminal," and "Empty" were each considered and rejected for concrete, verified reasons during this design's grilling session (see Solution above) - don't re-propose them without a reason those checks missed.
- `PublicAPI.Unshipped.txt` entries for every renamed public type/member need updating across affected packages, consistent with this project's existing rename precedent (e.g. `.scratch/outcome-naming-and-arity-refactor/issues/08`).
