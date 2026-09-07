# 07 - Reconcile `Interlocutor.Generator`'s documented wiring with reality

**What to build:** CLAUDE.md, README.md, and `documentation/glossary.md` accurately describe `Interlocutor.Generator` as an opt-in package a consumer references directly for compile-time handler discovery — matching the generator project's own comment and how `TutorialProject` actually uses it (via `ScanAssembly` instead) — rather than the current false claim that it rides in automatically as an analyzer reference on `Interlocutor.Abstractions`.

Decision made: fix the docs, not the csproj — `Interlocutor.Abstractions.csproj` keeps no generator reference; the compile-time path stays opt-in.

Full technical detail: [findings/04-generator-wiring-docs-mismatch.md](../findings/04-generator-wiring-docs-mismatch.md).

**Blocked by:** None - can start immediately.

**Status:** done

- [x] CLAUDE.md's description of `Interlocutor.Generator`'s wiring matches the actual project graph
- [x] README.md's equivalent claim (around the Interlocutor.Generator mention) is corrected the same way
- [x] `documentation/glossary.md`'s `Interlocutor.Generator` entry is corrected the same way
- [x] A `DependencyTests.cs` assertion pins the real behavior (no `Interlocutor.Generator` reference on `Interlocutor.Abstractions`) so this can't silently drift again

## Comments

**2026-09-04** - Re-triaged while working the frontier: already implemented in commit `6a3f9ce`. CLAUDE.md, README.md, and documentation/glossary.md all now describe Interlocutor.Generator as opt-in, not an analyzer Abstractions pulls in transitively. tests/Interlocutor.Architecture.Tests/DependencyTests.cs's Abstractions_ReferencedAssembly_HasNoDependencyOnGenerator_Test pins the real behavior. No source changes were needed this session.
