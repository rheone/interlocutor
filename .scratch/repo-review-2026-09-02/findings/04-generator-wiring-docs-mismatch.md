Status: needs-triage
Severity: HIGH
Category: architecture / documentation
Source: architecture.md lane, confirmed by antagonist lane (also found the same claim repeated in README.md)

# `Interlocutor.Generator`'s documented wiring on `Abstractions` doesn't exist in the project graph

## Summary

CLAUDE.md, README.md (line 97-99), and `documentation/glossary.md` all state `Interlocutor.Generator` "rides in as a `PrivateAssets=\"all\"` analyzer reference on `Interlocutor.Abstractions`" — but `Interlocutor.Abstractions.csproj` carries no reference to the generator at all. This is a three-doc-wide, self-reinforcing inaccuracy, not a one-off typo.

## Evidence

- `src/Interlocutor.Abstractions/Interlocutor.Abstractions.csproj`: exactly one `ProjectReference` (to `Interlocutor.CommonKernel`), nothing generator-related.
- The sole `src/` reference to `Interlocutor.Generator.csproj` is on `Interlocutor.CommonKernel.csproj`, whose own inline comment explains it exists only so `OutcomeMemberGenerator` can generate `Outcome`/`Outcome<...>`'s `Match`/`Switch`/etc. members for CommonKernel's *own* compilation — unrelated to handler-discovery.
- `Interlocutor.Generator.csproj`'s own comment confirms the real model: a consumer referencing `Interlocutor.Generator` gets it wired up as an analyzer automatically — i.e., a consumer adds that reference themselves; it is not transitively pulled in via `Interlocutor.Abstractions`.
- `TutorialProject/TodoTasks.Api` (zero references to `Interlocutor.Generator`) uses `ScanAssembly` (runtime reflection) instead, exactly as `documentation/beginners-guide-todo-api.md:1764` describes: "a separate, additive compile-time path via `Interlocutor.Generator`/`AddGeneratedRegistrations` that this guide doesn't use." The shipped sample app itself demonstrates that referencing `Interlocutor.Abstractions` alone does **not** get you compile-time handler discovery.
- `tests/Interlocutor.Architecture.Tests/DependencyTests.cs` has no assertion at all involving the Generator assembly, so nothing catches this claim drifting further.

## Suggested action

Either (a) correct CLAUDE.md/README.md/glossary.md to describe `Interlocutor.Generator` as an opt-in package a consumer references directly (matching the generator project's own comment and TutorialProject's actual usage), or (b) if the intent was always for `Interlocutor.Abstractions` to carry the analyzer automatically, add that `PrivateAssets="all"` reference to `Interlocutor.Abstractions.csproj` so the shipped package matches the documented architecture. Either way, add a `DependencyTests.cs` assertion pinning whichever behavior is chosen so this can't silently drift again.
