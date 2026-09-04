# 01 - Rename `IOperationOutcome`/`OperationOutcome<...>` to `IOutcome`/`Outcome<...>` (repo-wide)

**What to build:** `IOperationOutcome` and `OperationOutcome`/`OperationOutcome<...>` are the pervasive, load-bearing names for this library's marker interface and its built-in union implementation - but "Operation" is redundant (everything in this library is an operation's outcome by definition) and the pairing reads as two generic words stacked together. Rename both to `IOutcome` and `Outcome`/`Outcome<...>` everywhere in the repository: `src/`, `tests/`, `documentation/` (including `glossary.md`, every prose mention, and every code sample), `TutorialProject/`, and every `maintainer_docs/adr/*.md` that names the type.

This is a pure identifier rename with zero behavioral change - `IOperationOutcome` is literally `I` + `OperationOutcome`, so a single substring rename (`OperationOutcome` -> `Outcome`) correctly produces `IOutcome` from `IOperationOutcome` in the same pass. Treat it as one mechanical pass rather than batching by package: it's low-judgment, high-volume work verified by a full solution build and test run, not by careful per-file review. Matches this repo's existing precedent for pre-release renames (no shipped consumers yet) - no `[Obsolete]` forwarding shim, no compatibility alias left behind.

**Blocked by:** None - can start immediately.

**Status:** done (already implemented)

- [x] Every `IOperationOutcome` reference in `src/` and `tests/` is renamed to `IOutcome`.
- [x] Every `OperationOutcome`/`OperationOutcome<...>` reference (all six arities, including the non-generic form) in `src/` and `tests/` is renamed to `Outcome`/`Outcome<...>`, including test class/file names that embed the old name (e.g. `OperationOutcomeTests` -> `OutcomeTests`, `OperationOutcomeOfTwoCasesTests` -> `OutcomeOfTwoCasesTests`, and so on for every arity).
- [x] `documentation/glossary.md` and every other file under `documentation/` (prose and code samples alike) use the new names; no stale `OperationOutcome`/`IOperationOutcome` mention remains anywhere under `documentation/`.
- [x] `TutorialProject/` (source, tests, and any prose/comments referencing the type) is fully updated to the new names.
- [x] Every `maintainer_docs/adr/*.md` that names the type is updated (rename the identifier in place; do not rewrite the ADR's historical narrative beyond the name change).
- [x] `rg -n "OperationOutcome"` across the whole repository (excluding `.git/`, `obj/`, `bin/`) returns zero matches (outside historical `.scratch/` ticket prose, which predates the rename and is out of scope).
- [x] `dotnet build` and `dotnet test` pass for the whole solution; `dotnet csharpier .` produces no diff.

## Comments

**2026-09-04** - Re-triaged while working the frontier: the rename had already landed in commit `54fd6fe` ("refactor: rename IOperationOutcome/OperationOutcome to IOutcome/Outcome"), an ancestor of current `HEAD`, but this ticket's Status was never flipped during normalization. Verified: repo-wide `rg -n "OperationOutcome"` (excluding `.git/`, `obj/`, `bin/`) returns zero matches outside `.scratch/`. Full solution build and test (1189/1189) green. Also fixed two stray stale-comment references to `OperationOutcome` found in this pass (`.editorconfig` and a csharp-code-organization skill reference doc) - not in the ticket's named scope but caught by its own zero-matches acceptance criterion.

