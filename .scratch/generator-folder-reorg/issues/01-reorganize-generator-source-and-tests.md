# 01 - Reorganize Interlocutor.Generator into responsibility folders

**What to build:** `src/Interlocutor.Generator` is currently a flat list of 38 files at the project root - the only multi-concern `src/*` project laid out this way. Every other one (e.g. `Interlocutor.Abstractions`, with `Commands/`, `Queries/`, `Pipeline/`, `Markers/`, `Notifications/`, `Diagnostics/`, `Core/`) groups its files by responsibility, with the folder mirrored in the namespace (`.editorconfig` already turns on `dotnet_style_namespace_match_folder`). Bring `Interlocutor.Generator` in line with that pattern.

Group by what each type does in the generator pipeline:

- **`Models/`** - the equatable, incremental-pipeline-safe snapshot/data types and their discriminator enums (what the pipeline passes between stages).
- **`Analysis/`** - semantic-model analysis over the compilation: builders, resolvers, planners, and analyzer/discovery types that turn Roslyn symbols into models or diagnostics.
- **`Emission/`** - types that emit generated C# source text from a model.
- **`Diagnostics/`** - the diagnostic descriptor catalog (mirrors `Interlocutor.Abstractions/Diagnostics/`, also a single-file folder there).
- **Stays at the project root**, namespace unchanged: the two standalone `[Generator]`/`IIncrementalGenerator` entry points (`InterlocutorSourceGenerator`, `OutcomeMemberGenerator` - `tests/Interlocutor.Generator.Tests/GeneratorTestHarness.cs` references both unqualified, relying on its namespace being a child of `Interlocutor.Generator`; moving or renamespacing either breaks that and drags test changes out of scope), plus cross-cutting utilities used from both `Analysis/` and `Emission/` (symbol formatting, well-known type name lookup, identifier sanitizing) and the netstandard2.0 compiler polyfill.

All 38 types are `internal` except the two entry points above, and the project uses default SDK-style file globbing (no explicit `<Compile Include>` items to update) - this is a self-contained move, not a public-API change.

Mirror the same grouping in `tests/Interlocutor.Generator.Tests/` where a test file's target is clearly a single one of those responsibilities (e.g. tests asserting on a model's `DebuggerDisplay` output belong with `Models/`-equivalent tests; tests asserting on emitted source text belong with `Emission/`-equivalent tests; analyzer/diagnostic tests belong with `Analysis/`- or `Diagnostics/`-equivalent tests). That project currently tests by scenario rather than by source file, so several tests exercise the full pipeline end-to-end (discovery through emission) or span cross-assembly scenarios - leave those, and the shared `GeneratorTestHarness.cs`, ungrouped at the test project's root rather than forcing them into a folder that misrepresents what they cover.

**Blocked by:** None - can start immediately.

**Status:** done

- [x] `src/Interlocutor.Generator`'s files are grouped into `Models/`, `Analysis/`, `Emission/`, `Diagnostics/` per the responsibilities above, each folder's files namespaced to match (e.g. `Interlocutor.Generator.Models`)
- [x] `InterlocutorSourceGenerator.cs` and `OutcomeMemberGenerator.cs` remain at the project root in the `Interlocutor.Generator` namespace, unmoved
- [x] `dotnet build` succeeds solution-wide with no new warnings
- [x] `dotnet test` passes solution-wide, including `tests/Interlocutor.Generator.Tests` and the downstream projects whose generated code depends on this assembly (`Interlocutor.DependencyInjection.*.Tests`, `TodoTasks.Api.Tests`)
- [x] `tests/Interlocutor.Generator.Tests` is regrouped to mirror the same responsibility split wherever a test file maps cleanly to one; whole-pipeline/cross-assembly tests and the shared harness are left at the project root
- [x] `dotnet csharpier .` has been run and produces no further diff
- [x] `git diff --stat` shows only renames plus the expected namespace/using-directive edits - no incidental content changes

## Comments

**2026-09-04** - Verified on `2026-09-04-ResearchTicketNormalization` after merging `origin/2026-09-04-CodebaseAuditFollowUps` and `origin/ultrareview`: `src/Interlocutor.Generator/` is split into `Models/`, `Analysis/`, `Emission/`, and `Diagnostics/` as specified. Landed via `origin/ultrareview` commit `8b2d7da`, merged into this branch. Full solution `dotnet build` and `dotnet test` (1189/1189) pass post-merge.
