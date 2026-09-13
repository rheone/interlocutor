# 02 — Apply the using-directive and SA1200 configuration decisions

**What to build:** Three `.editorconfig` changes, decided in
`.scratch/analyzer-formatting-alignment/issues/01-findings-and-decision-document.md` (Findings 1a, 1b,
1c). Finding 2 in that document resolved to no change — nothing to do for it here.

**Blocked by:** 01 — done, all four decisions recorded.

**Status:** ready-for-agent

- [ ] `dotnet_separate_import_directive_groups` flipped from `false` to `true` in `.editorconfig`'s
  `#### .NET Coding Conventions ####` section (decision 1a).
- [ ] `dotnet_diagnostic.SA1208.severity = Warning` and `dotnet_diagnostic.SA1210.severity = Warning`
  added explicitly to `.editorconfig`, matching the SA1600-family's existing pattern of recorded,
  commented severities rather than an unstated shipped default (decision 1b). Not raised to `Error`.
- [ ] Before touching SA1200: re-run the repo-wide scan of every source `.cs` file's `using`-vs-`namespace`
  line order across `src/`, `tests/`, `TutorialProject/`, and `smoketests/` (excluding generated `obj/`/`bin/`
  output) that issue 01 already ran, to confirm the `tests/`-outside-namespace split it describes still
  doesn't exist. If it's still zero files, proceed; if a file now violates it, stop and report back rather
  than silently working around it.
- [ ] `dotnet_diagnostic.SA1200.severity` restored to `Error` in `.editorconfig`, and the stale "Warning,
  not error, until tests/ is migrated" comment immediately above it deleted (decision 1c).
- [ ] `dotnet csharpier .` run afterward to absorb the reformat churn from flipping
  `dotnet_separate_import_directive_groups` (every multi-group using block in the repo where `System.*`
  usings sit next to non-`System.*` ones gets a blank line inserted between the groups).
- [ ] `dotnet build` succeeds solution-wide after all four changes — in particular, confirm SA1200's
  `Error` severity doesn't turn up an unexpected pre-existing violation the scan missed.
- [ ] `dotnet test` passes with no new failures (pre-existing, unrelated failures in `TodoTasks.Api.Tests`
  tied to uncommitted package-version changes, if still present at implementation time, are out of scope).

## Comments

Filed following the grilling session recorded against `documentation/../.scratch/analyzer-formatting-alignment/issues/01-findings-and-decision-document.md`'s four decisions. Finding 2 (missing-XML-comment
enforcement scope) resolved to **no change** — see that document's Finding 2 decision write-up for the
full reasoning, including the considered-and-rejected `DiagnosticSuppressor` approach and the documented
fallback design if member-level "encouraged, not required" for test methods becomes worth building later.
