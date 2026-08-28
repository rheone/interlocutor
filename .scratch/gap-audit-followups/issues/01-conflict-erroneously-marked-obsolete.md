# 01 - `Conflict`/`Conflicts` erroneously marked `[Obsolete]`

**What to build:** Remove the stray `[Obsolete("May be overkill to include")]` attribute from `Interlocutor.CommonKernel.ResultTypes.Conflict` and `.Conflicts`.

**Found during:** a fresh gap-finding pass comparing shipped `src/` against `CLAUDE.md`/README/glossary (2026-08-28), not filed against a prior ticket.

**Detail:** `Conflict` and `Conflicts` are two of CLAUDE.md's, README's, and `documentation/glossary.md`'s canonical, documented `OperationOutcome` failure cases - never described as deprecated or optional anywhere. `git blame` traces the attribute to `ce1b819` ("refactor: move result types into ResultTypes namespace, fix build"), a mechanical namespace-move commit with no stated intent to deprecate anything. The same commit correctly carried `[Obsolete("May be overkill to include")]` on `Cancelled`, `Timeout`, and `RateLimited` - three new, genuinely-debatable case types added in `resulttypes-cleanup/05` specifically because they don't parallel the original five HTTP-shaped cases. That attribute leaked onto the pre-existing `Conflict`/`Conflicts` files during the move, most likely a copy/paste slip between sibling files in the same batch of edits.

**Impact:** every production usage of `Conflict`/`Conflicts` inside the library's own shipped code emits CS0618 - confirmed in `Interlocutor.Abstractions/Pipeline/Classifiers/OutcomeClassifierBuilder.cs` (3 call sites) and `Interlocutor.AspNetCore/DefaultOutcomeStatusMapper.cs` (5 call sites) via a forced rebuild (`dotnet build -t:Rebuild`) - these warnings don't surface on every `dotnet build` only because MSBuild skips up-to-date projects, which is how this went unnoticed. Any consumer application constructing a `Conflict` outcome - the documented, load-bearing way to report a state conflict - gets the same false "may be overkill to include" warning on entirely ordinary use of a first-class, undeprecated type.

**Blocked by:** None - can start immediately.

**Status:** closed (implemented)

## Acceptance criteria

- [x] `[Obsolete]` removed from `Conflict` (`src/Interlocutor.CommonKernel/ResultTypes/Conflict.cs`)
- [x] `[Obsolete]` removed from `Conflicts` (`src/Interlocutor.CommonKernel/ResultTypes/Conflicts.cs`)
- [x] `Cancelled`, `Timeout`, `RateLimited` keep their `[Obsolete]` attribute unchanged - this ticket narrows scope, it doesn't relitigate whether those three should exist
- [x] A forced full rebuild (`dotnet build -t:Rebuild`) shows zero CS0618 warnings referencing `Conflict`/`Conflicts` anywhere in `src/` or `tests/`
- [x] Full test suite passes

## Comments

Verified via `git blame`/`git log -p` that the attribute was introduced in `ce1b819`, a pure namespace-move refactor with no deprecation intent in its commit message, and that no doc/ADR anywhere describes `Conflict`/`Conflicts` as deprecated - confirms this was accidental, not a reversed decision. Fix was a two-line deletion (the `[Obsolete]` attribute line in each file); no other code, tests, or docs referenced the attribute. Forced rebuild of `Interlocutor.CommonKernel`, `Interlocutor.Abstractions`, and `Interlocutor.AspNetCore` confirmed the CS0618 warnings for `Conflict`/`Conflicts` are gone while the three intentionally-obsolete types (`Cancelled`/`Timeout`/`RateLimited`) still warn. Full suite: see commit for pass count.
